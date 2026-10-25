using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TiemChung.Api;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class IdentityWriteRevokeSqlTests
{
    [Theory,Trait("Category","SqlIntegration")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AccountLockSerializesFinalWriteWithRevocation(bool writeFirst)
    {
        var cs=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")??throw new InvalidOperationException("SQL required.")) {
            InitialCatalog="KLCN_M1_Test_"+Guid.NewGuid().ToString("N") };
        var options=new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs.ConnectionString).Options;
        await using var setup=new TiemChungDbContext(options); await setup.Database.MigrateAsync();
        var now=DateTimeOffset.UtcNow;
        var a=new TaiKhoan { AccountType="Customer",LoginEmail="write@example.invalid",NormalizedEmail="WRITE@EXAMPLE.INVALID",TenDangNhap="synthetic-write",MatKhauHash="not-login",TrangThai="Active",EmailVerifiedAt=now,CreatedAt=now };
        setup.Add(a); await setup.SaveChangesAsync();
        var s=new IdentitySession { Id=Guid.NewGuid(),AccountId=a.MaTaiKhoan,SecurityVersion=1,ClientType="native",CreatedAt=now,ExpiresAt=now.AddDays(7) };
        setup.Add(s); await setup.SaveChangesAsync();
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Identity:SigningKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) }).Build();
        var tokens=new IdentityTokens(config);
        await using var first=new TiemChungDbContext(options); await using var second=new TiemChungDbContext(options);
        await using var transaction=await IdentityTransactions.BeginAsync(first,CancellationToken.None);
        var locked=await IdentityTransactions.LockAccountAsync(first,a.MaTaiKhoan,CancellationToken.None);
        async Task<bool> Write(TiemChungDbContext db)
        {
            var context=new Microsoft.AspNetCore.Http.DefaultHttpContext(); context.Request.Headers.Authorization="Bearer "+tokens.Issue(a.MaTaiKhoan,s.Id,now);
            try {
                await new IdentityAuthorization(db,tokens).RequireAsync(context,true,CancellationToken.None);
                db.Add(new KhachHang { HoTen="Synthetic Serialized",CCCD="000000000001",SoDienThoai="0900000000",Email="write@example.invalid" });
                IdentityTransactions.Audit(db,"profiles.created","synthetic-write",a.MaTaiKhoan);
                await db.SaveChangesAsync(); return true;
            } catch(IdentityFailure f) when(f.Status==401) { return false; }
        }
        if(writeFirst) {
            Assert.True(await Write(first));
            var revoke=Task.Run(async()=> {
                await using var tx=await IdentityTransactions.BeginAsync(second,CancellationToken.None);
                var account=await IdentityTransactions.LockAccountAsync(second,a.MaTaiKhoan,CancellationToken.None);
                await IdentityTransactions.RevokeAccountAsync(second,account!,"test","revoke",null,CancellationToken.None);
                await second.SaveChangesAsync(); await tx.CommitAsync();
            });
            await transaction.CommitAsync(); await revoke;
        } else {
            await IdentityTransactions.RevokeAccountAsync(first,locked!,"test","revoke",null,CancellationToken.None);
            await first.SaveChangesAsync(); await transaction.CommitAsync();
            await using var tx=await IdentityTransactions.BeginAsync(second,CancellationToken.None);
            Assert.False(await Write(second));
        }
        Assert.Equal(writeFirst?1:0,await setup.Customers.CountAsync());
    }
}
