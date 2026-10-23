using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TiemChung.Api;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class IdentityMailSqlTests
{
    [Fact,Trait("Category","SqlIntegration")]
    public async Task SinkRetriesAndResendInvalidatesOldTokenWithoutChangingPassword()
    {
        var source=Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")??throw new InvalidOperationException("SQL required.");
        var cs=new SqlConnectionStringBuilder(source) { InitialCatalog="KLCN_M1_Test_"+Guid.NewGuid().ToString("N") };
        await using var db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs.ConnectionString).Options);
        await db.Database.MigrateAsync();
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Identity:MailEncryptionKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
        }).Build();
        var registration=new IdentityRegistration(db,config);
        await registration.RegisterAsync("mail@example.invalid","synthetic-original-password","register",CancellationToken.None);
        var hash=(await db.Set<TaiKhoan>().AsNoTracking().SingleAsync()).MatKhauHash;
        await registration.RegisterAsync("mail@example.invalid","synthetic-changed-password","repeat",CancellationToken.None);
        Assert.Equal(hash,(await db.Set<TaiKhoan>().AsNoTracking().SingleAsync()).MatKhauHash);
        var sink=new Sink { Fail=true };
        var dispatcher=new IdentityMailSink(db,config,new Env());
        await dispatcher.DrainAsync(sink,CancellationToken.None);
        Assert.Empty(sink.Delivered);
        var row=await db.Set<IdentityMailOutbox>().SingleAsync();
        Assert.Equal(1,row.Attempts); Assert.NotNull(row.EncryptedPayload);
        row.NextAttemptAt=null; await db.SaveChangesAsync(); sink.Fail=false;
        await dispatcher.DrainAsync(sink,CancellationToken.None);
        Assert.Null(row.EncryptedPayload); Assert.NotNull(row.SentAt);
        var old=sink.Delivered.Single().Value;
        await registration.ResendAsync("mail@example.invalid","resend",CancellationToken.None);
        Assert.Equal("invalidVerificationToken",(await Assert.ThrowsAsync<IdentityFailure>(()=>registration.ConfirmAsync(old,"old",CancellationToken.None))).Code);
        db.ChangeTracker.Clear();
        await dispatcher.DrainAsync(sink,CancellationToken.None);
        var fresh=sink.Delivered.Values.Single(x=>x!=old);
        await registration.ConfirmAsync(fresh,"confirm",CancellationToken.None);
        Assert.Equal("invalidVerificationToken",(await Assert.ThrowsAsync<IdentityFailure>(()=>registration.ConfirmAsync(fresh,"used",CancellationToken.None))).Code);
        Assert.Empty(await db.Set<AccountProfileLink>().ToListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(()=>new IdentityMailSink(db,config,new Env { EnvironmentName="Production" }).DrainAsync(sink,CancellationToken.None));
    }
    private sealed class Sink : IVerificationMailSink
    {
        public bool Fail { get; set; }
        public Dictionary<Guid,string> Delivered { get; }=[];
        public Task DeliverAsync(Guid id,string address,string token,CancellationToken ct)
        {
            if(Fail) throw new InvalidOperationException("Synthetic failure");
            Assert.EndsWith("@example.invalid",address); Delivered.TryAdd(id,token); return Task.CompletedTask;
        }
    }
    private sealed class Env : IHostEnvironment
    {
        public string EnvironmentName { get; set; }="Test";
        public string ApplicationName { get; set; }="Synthetic";
        public string ContentRootPath { get; set; }="";
        public IFileProvider ContentRootFileProvider { get; set; }=new NullFileProvider();
    }
}
