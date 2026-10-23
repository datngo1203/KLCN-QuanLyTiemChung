using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TiemChung.Api;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class MailLifecycleSqlTests
{
    [Fact,Trait("Category","SqlIntegration")]
    public async Task ProductionIntakeIsClosedEvenWithSyntheticFlag()
    {
        var cs=Connection();
        await using var db=Db(cs); await db.Database.MigrateAsync();
        await using var host=Host(cs,"Production"); using var client=host.CreateClient();
        foreach(var path in new[]{"register","email-verifications/resend"}) {
            var response=await client.PostAsJsonAsync("/api/v1/auth/"+path,path=="register"?(object)new {email="p@example.invalid",password="synthetic-password"}:new {email="p@example.invalid"});
            Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);
            Assert.Contains("dependencyUnavailable",await response.Content.ReadAsStringAsync());
        }
        Assert.Empty(await db.Set<TaiKhoan>().ToListAsync());
        Assert.Empty(await db.Set<IdentityMailOutbox>().ToListAsync());
    }

    [Fact,Trait("Category","SqlIntegration")]
    public async Task DeliverySideEffectSurvivesRollbackAcrossIndependentSinks()
    {
        var cs=Connection(); await using var db=Db(cs); await db.Database.MigrateAsync();
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Identity:MailEncryptionKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) }).Build();
        await new IdentityRegistration(db,config).RegisterAsync("race@example.invalid","synthetic-password","race",default);
        var id=(await db.Set<IdentityMailOutbox>().SingleAsync()).Id;
        var a=new SyntheticVerificationSink(); var b=new SyntheticVerificationSink();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailMailCommit ON IdentityMailOutbox AFTER UPDATE AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE SentAt IS NOT NULL) THROW 51009, 'Synthetic failure', 1; END;");
        await Assert.ThrowsAsync<DbUpdateException>(()=>new IdentityMailSink(db,config,new TestEnvironment()).DrainAsync(a,default));
        db.ChangeTracker.Clear();
        Assert.Single(a.Messages); Assert.Null((await db.Set<IdentityMailOutbox>().SingleAsync()).SentAt);
        Assert.NotNull((await db.Set<IdentityMailOutbox>().SingleAsync()).EncryptedPayload);
        // Same instance retry after accepted+rollback still has only one accepted message.
        await Assert.ThrowsAsync<DbUpdateException>(()=>new IdentityMailSink(db,config,new TestEnvironment()).DrainAsync(a,default));
        Assert.Single(a.Messages); db.ChangeTracker.Clear();
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailMailCommit;");
        await using var hostB=Db(cs);
        await new IdentityMailSink(hostB,config,new TestEnvironment()).DrainAsync(b,default);
        Assert.Single(b.Messages); Assert.Equal(2,a.Messages.Count+b.Messages.Count);
        Assert.Equal(id,b.Messages.Single().Key); Assert.Equal(a.Messages[id].Token,b.Messages[id].Token);
        db.ChangeTracker.Clear(); var sent=await db.Set<IdentityMailOutbox>().SingleAsync();
        Assert.NotNull(sent.SentAt); Assert.Null(sent.EncryptedPayload);
        var auth=new IdentityRegistration(db,config);
        await auth.ConfirmAsync(b.Messages[id].Token,"confirm",default);
        await Assert.ThrowsAsync<IdentityFailure>(()=>auth.ConfirmAsync(b.Messages[id].Token,"again",default));
        Assert.Equal(1,await db.Set<TaiKhoan>().CountAsync()); Assert.Empty(await db.Set<AccountProfileLink>().ToListAsync());
        await auth.RegisterAsync("concurrent@example.invalid","synthetic-password","race-two",default);
        var shared=new SyntheticVerificationSink();
        async Task Drain() {
            await using var connection=Db(cs);
            await new IdentityMailSink(connection,config,new TestEnvironment()).DrainAsync(shared,default);
        }
        await Task.WhenAll(Drain(),Drain()); Assert.Single(shared.Messages);
    }

    [Theory,Trait("Category","SqlIntegration")]
    [InlineData(false,"Synthetic")]
    [InlineData(true,"Disabled")]
    public async Task MissingMarkerOrModeRejectsIntakeBeforeWrites(bool marker,string mode)
    {
        var cs=Connection(); await using var db=Db(cs); await db.Database.MigrateAsync();
        if(marker) await IdentityFixture.MarkSyntheticAsync(db);
        await using var host=Host(cs,"Test").WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?> { ["Identity:MailMode"]=mode })));
        using var client=host.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync("/api/v1/auth/register",new {email="no@example.invalid",password="synthetic-password"})).StatusCode);
        Assert.Empty(await db.Set<TaiKhoan>().ToListAsync()); Assert.Empty(await db.Set<IdentityMailOutbox>().ToListAsync());
    }

    [Fact,Trait("Category","SqlIntegration")]
    public async Task HostRetriesDeliveryAndStopsWorkerWithoutManualDrain()
    {
        var cs=Connection(); await using var db=Db(cs); await db.Database.MigrateAsync(); await IdentityFixture.MarkSyntheticAsync(db);
        var sink=new FailOnceSink();
        await using var host=Host(cs,"Test",sink); using var client=host.CreateClient();
        Assert.Equal(HttpStatusCode.Accepted,(await client.PostAsJsonAsync("/api/v1/auth/register",new {email="live@example.invalid",password="synthetic-password"})).StatusCode);
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while(Volatile.Read(ref sink.Attempts)==0) await Task.Delay(50,timeout.Token);
        // Accelerate persisted retry deadline, not dispatcher invocation.
        while(!await db.Set<IdentityMailOutbox>().AnyAsync(x=>x.Attempts==1)) await Task.Delay(50,timeout.Token);
        await db.Set<IdentityMailOutbox>().ExecuteUpdateAsync(s=>s.SetProperty(x=>x.NextAttemptAt,(DateTimeOffset?)DateTimeOffset.UtcNow));
        while(sink.Inner.Messages.Count==0) await Task.Delay(50,timeout.Token);
        var token=sink.Inner.Messages.Single().Value.Token;
        Assert.Equal(HttpStatusCode.NoContent,(await client.PostAsJsonAsync("/api/v1/auth/email-verifications/confirm",new {token})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/v1/auth/login",new {identifier="live@example.invalid",password="synthetic-password",clientType="native"})).StatusCode);
        var readiness=host.Services.GetRequiredService<IdentityMailReadiness>();
        await host.DisposeAsync(); Assert.False(readiness.Running);
    }

    private static string Connection()=>new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")??throw new InvalidOperationException("SQL required")) { InitialCatalog="KLCN_M1_Test_"+Guid.NewGuid().ToString("N") }.ConnectionString;
    private static TiemChungDbContext Db(string cs)=>new(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs).Options);
    private static WebApplicationFactory<Program> Host(string cs,string env,IVerificationMailSink? sink=null)=>new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseEnvironment(env).UseSetting("ConnectionStrings:TiemChung",cs).UseSetting("Catalog:UseDemoData","false").ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?> {
        ["ConnectionStrings:TiemChung"]=cs,["Catalog:UseDemoData"]="false",["Identity:MailMode"]="Synthetic",
        ["Identity:SigningKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
        ["Identity:MailEncryptionKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
    })).ConfigureServices(s=> { s.RemoveAll<DbContextOptions<TiemChungDbContext>>();s.RemoveAll<TiemChungDbContext>();s.AddDbContext<TiemChungDbContext>(o=>o.UseSqlServer(cs)); if(sink is not null){s.RemoveAll<IVerificationMailSink>();s.AddSingleton(sink);} }));
    private sealed class FailOnceSink : IVerificationMailSink {
        public int Attempts; public SyntheticVerificationSink Inner {get;}=new();
        public Task DeliverAsync(Guid id,string address,string token,CancellationToken ct) {
            if(Interlocked.Increment(ref Attempts)==1) throw new InvalidOperationException("Synthetic failure");
            return Inner.DeliverAsync(id,address,token,ct);
        }
    }
    private sealed class TestEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment {
        public string EnvironmentName {get;set;}="Test";public string ApplicationName {get;set;}="Test";public string ContentRootPath {get;set;}="";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider {get;set;}=new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
