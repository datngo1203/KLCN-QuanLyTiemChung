using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

// Run explicitly with Category=SqlIntegration. Missing SQL is a failure, never a pass/skip.
public sealed class IdentitySqlTests
{
    [Fact, Trait("Category","SqlIntegration")]
    public async Task RegisterConfirmRotateReplayAndNextRequestRevocationUseRealSql()
    {
        var source=Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")??throw new InvalidOperationException("SQL required.");
        var cs=new SqlConnectionStringBuilder(source) { InitialCatalog="KLCN_M1_Test_"+Guid.NewGuid().ToString("N") };
        var options=new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs.ConnectionString).Options;
        await using var db=new TiemChungDbContext(options); await db.Database.MigrateAsync();
        var config=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Identity:SigningKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["Identity:MailEncryptionKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
        }).Build();
        var registration=new TiemChung.Api.IdentityRegistration(db,config);
        await registration.RegisterAsync("person@example.invalid","synthetic-passphrase-123","test-register",CancellationToken.None);
        Assert.Empty(await db.Customers.ToListAsync());
        var outbox=await db.Set<IdentityMailOutbox>().SingleAsync();
        var payload=outbox.EncryptedPayload!; var plaintext=new byte[payload.Length-28];
        using(var aes=new System.Security.Cryptography.AesGcm(Convert.FromBase64String(config["Identity:MailEncryptionKey"]!),16))
            aes.Decrypt(payload.AsSpan(0,12),payload.AsSpan(28),payload.AsSpan(12,16),plaintext);
        using var json=System.Text.Json.JsonDocument.Parse(plaintext);
        var verification=json.RootElement.GetProperty("token").GetString()!;
        await registration.ConfirmAsync(verification,"test-confirm",CancellationToken.None);
        var tokens=new TiemChung.Api.IdentityTokens(config);
        db.ChangeTracker.Clear();
        var sessions=new TiemChung.Api.IdentitySessions(db,tokens);
        var login=await sessions.LoginAsync("person@example.invalid","synthetic-passphrase-123","native","test-login",CancellationToken.None);
        db.ChangeTracker.Clear();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectRefreshAudit ON SecurityAudit AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action='refreshRotated') THROW 51009, 'Synthetic audit failure', 1; END;");
        await Assert.ThrowsAsync<DbUpdateException>(()=>sessions.RefreshAsync(login.RefreshToken,"native",null,"test-audit-failure",CancellationToken.None));
        db.ChangeTracker.Clear();
        Assert.Equal(1,await db.Set<RefreshCredential>().CountAsync(x=>x.SessionId==login.SessionId && x.ConsumedAt==null && x.RevokedAt==null));
        Assert.Equal(1,await db.Set<RefreshCredential>().CountAsync(x=>x.SessionId==login.SessionId));
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectRefreshAudit;");
        async Task<(TiemChung.Api.SessionTokens? Tokens,int Status)> Compete()
        {
            await using var connection=new TiemChungDbContext(options);
            try { return (await new TiemChung.Api.IdentitySessions(connection,tokens).RefreshAsync(login.RefreshToken,"native",null,"test-parallel",CancellationToken.None),200); }
            catch(TiemChung.Api.IdentityFailure failure) { return (null,failure.Status); }
        }
        var results=await Task.WhenAll(Compete(),Compete());
        Assert.Equal(new[]{200,409},results.Select(x=>x.Status).Order().ToArray());
        var rotated=results.Single(x=>x.Status==200).Tokens!;
        Assert.NotEqual(login.RefreshToken,rotated.RefreshToken);
        db.ChangeTracker.Clear();
        var competing=await Assert.ThrowsAsync<TiemChung.Api.IdentityFailure>(()=>sessions.RefreshAsync(login.RefreshToken,"native",null,"test-compete",CancellationToken.None));
        Assert.Equal(409,competing.Status);
        await db.Set<RefreshCredential>().Where(x=>x.ConsumedAt!=null).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.ConsumedAt,DateTimeOffset.UtcNow.AddSeconds(-6)));
        db.ChangeTracker.Clear();
        var replay=await Assert.ThrowsAsync<TiemChung.Api.IdentityFailure>(()=>sessions.RefreshAsync(login.RefreshToken,"native",null,"test-replay",CancellationToken.None));
        Assert.Equal("refreshReplay",replay.Code);
        Assert.Equal(0,await db.Set<RefreshCredential>().CountAsync(x=>x.SessionId==login.SessionId && x.ConsumedAt==null && x.RevokedAt==null));
        db.ChangeTracker.Clear();
        var context=new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Headers.Authorization="Bearer "+rotated.AccessToken;
        var authorization=new TiemChung.Api.IdentityAuthorization(db,tokens);
        Assert.Equal(401,(await Assert.ThrowsAsync<TiemChung.Api.IdentityFailure>(()=>authorization.RequireAsync(context,false,CancellationToken.None))).Status);
        db.ChangeTracker.Clear();
        var logoutSession=await sessions.LoginAsync("person@example.invalid","synthetic-passphrase-123","native","test-login-again",CancellationToken.None);
        db.ChangeTracker.Clear();
        var next=await sessions.RefreshAsync(logoutSession.RefreshToken,"native",null,"test-rotate-before-logout",CancellationToken.None);
        db.ChangeTracker.Clear();
        await sessions.LogoutAsync(logoutSession.RefreshToken,"native",null,"test-logout-old-token",CancellationToken.None);
        Assert.Equal(0,await db.Set<RefreshCredential>().CountAsync(x=>x.SessionId==logoutSession.SessionId && x.ConsumedAt==null && x.RevokedAt==null));
        db.ChangeTracker.Clear();
        Assert.Equal(401,(await Assert.ThrowsAsync<TiemChung.Api.IdentityFailure>(()=>sessions.RefreshAsync(next.RefreshToken,"native",null,"test-refresh-after-logout",CancellationToken.None))).Status);
        db.ChangeTracker.Clear();
        var firstLogout=await sessions.LoginAsync("person@example.invalid","synthetic-passphrase-123","native","test-login-third",CancellationToken.None);
        db.ChangeTracker.Clear();
        await sessions.LogoutAsync(firstLogout.RefreshToken,"native",null,"test-logout-first",CancellationToken.None);
        db.ChangeTracker.Clear();
        Assert.Equal(401,(await Assert.ThrowsAsync<TiemChung.Api.IdentityFailure>(()=>sessions.RefreshAsync(firstLogout.RefreshToken,"native",null,"test-rotate-after-logout",CancellationToken.None))).Status);
    }
    [Fact, Trait("Category","SqlIntegration")]
    public async Task FreshDatabaseMigratesAndEnforcesCredentialAndRecoveryUniqueness()
    {
        var connection=Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")
            ?? throw new InvalidOperationException("M1_SQL_TEST_CONNECTION required: SQL suite NOT VERIFIED.");
        var builder=new SqlConnectionStringBuilder(connection);
        // Never migrate a supplied shared DB: replace it with a unique synthetic database.
        builder.InitialCatalog="KLCN_M1_Test_"+Guid.NewGuid().ToString("N");
        await using var db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>()
            .UseSqlServer(builder.ConnectionString).Options);
        await db.Database.MigrateAsync();
        Assert.Contains("20261023073816_M1Identity",await db.Database.GetAppliedMigrationsAsync());
        Assert.Single(await db.Set<IdentityRecoveryDatabase>().ToListAsync());
        var now=DateTimeOffset.UtcNow;
        var account=new TaiKhoan { AccountType="Customer",TenDangNhap="synthetic-"+Guid.NewGuid().ToString("N"),
            LoginEmail="fixture@example.invalid",NormalizedEmail="FIXTURE@EXAMPLE.INVALID",MatKhauHash="synthetic-not-login",
            TrangThai="Active",CreatedAt=now };
        db.Add(account); await db.SaveChangesAsync();
        Assert.NotEmpty(account.RowVersion);
        var session=new IdentitySession { Id=Guid.NewGuid(),AccountId=account.MaTaiKhoan,SecurityVersion=1,
            ClientType="native",CreatedAt=now,ExpiresAt=now.AddDays(7) };
        db.Add(session); await db.SaveChangesAsync();
        db.Add(new RefreshCredential { Id=Guid.NewGuid(),SessionId=session.Id,TokenHash=System.Security.Cryptography.RandomNumberGenerator.GetBytes(32),
            CreatedAt=now,ExpiresAt=session.ExpiresAt });
        await db.SaveChangesAsync();
        db.Add(new RefreshCredential { Id=Guid.NewGuid(),SessionId=session.Id,TokenHash=System.Security.Cryptography.RandomNumberGenerator.GetBytes(32),
            CreatedAt=now,ExpiresAt=session.ExpiresAt });
        await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        await using var tx=await IdentityTransactions.BeginAsync(db,CancellationToken.None);
        var locked=await IdentityTransactions.LockAccountAsync(db,account.MaTaiKhoan,CancellationToken.None);
        await IdentityTransactions.RevokeAccountAsync(db,locked!,"test",Guid.NewGuid().ToString("N"),null,CancellationToken.None);
        await db.SaveChangesAsync(); await tx.CommitAsync();
        Assert.Equal(0,await db.Set<RefreshCredential>().CountAsync(x=>x.SessionId==session.Id && x.ConsumedAt==null && x.RevokedAt==null));
        // Retain only uniquely named synthetic DB for diagnostic inspection; never drop shared data.
    }
}
