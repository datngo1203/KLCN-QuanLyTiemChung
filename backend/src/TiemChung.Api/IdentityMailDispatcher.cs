using System.Collections.Concurrent;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public sealed class SyntheticVerificationSink(IHostEnvironment? environment = null, IConfiguration? config = null) : IVerificationMailSink
{
    private readonly ConcurrentDictionary<Guid,(string Address,string Token)> messages=new();
    // Internal host state for test/dev inspection through DI only, never mapped to HTTP/logs.
    public IReadOnlyDictionary<Guid,(string Address,string Token)> Messages=>messages;
    public async Task DeliverAsync(Guid deliveryId,string syntheticAddress,string token,CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if(!IdentityMailReadiness.IsSynthetic(syntheticAddress)) throw new InvalidOperationException("Synthetic recipient required.");
        if (environment?.IsDevelopment() == true && config?["Identity:DevelopmentMailDirectory"] is { Length: > 0 } directory) {
            if (!Path.IsPathFullyQualified(directory)) throw new InvalidOperationException("Absolute mail directory required.");
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, $"{deliveryId}.json"),
                System.Text.Json.JsonSerializer.Serialize(new { email = syntheticAddress, token }), ct);
        }
        messages.TryAdd(deliveryId,(syntheticAddress,token));
    }
}

public sealed class IdentityMailReadiness(IHostEnvironment env,IConfiguration config)
{
    public bool Running { get; set; }
    public bool Configured {
        get {
            if((!env.IsDevelopment() && !env.IsEnvironment("Test")) || config["Identity:MailMode"]!="Synthetic") return false;
            try { return Convert.FromBase64String(config["Identity:MailEncryptionKey"]??"").Length==32; }
            catch(FormatException) { return false; }
        }
    }
    public static bool IsSynthetic(string email)=>MailAddress.TryCreate(email,out var address) &&
        address.Address==email && address.Host.Equals("example.invalid",StringComparison.OrdinalIgnoreCase);
    public async Task RequireAsync(TiemChungDbContext db,string email,CancellationToken ct)
    {
        if(!Running || !Configured) throw new IdentityFailure(503,"dependencyUnavailable");
        if (env.IsDevelopment() && config["Identity:DevelopmentMailDirectory"] is { Length: > 0 } directory && Path.IsPathFullyQualified(directory)) {
            if (!IsSynthetic(email)) throw new IdentityFailure(400, "validation");
            return;
        }
        try {
            if(!db.Database.GetDbConnection().Database.StartsWith("KLCN_M1_Test_",StringComparison.Ordinal) ||
                await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM FixtureOnly WHERE Id=1").SingleAsync(ct)!=1)
                throw new IdentityFailure(503,"dependencyUnavailable");
        } catch(Microsoft.Data.SqlClient.SqlException) { throw new IdentityFailure(503,"dependencyUnavailable"); }
        if(!IsSynthetic(email)) throw new IdentityFailure(400,"validation");
    }
}

public sealed class IdentityMailDispatcher(IServiceScopeFactory scopes,IdentityMailReadiness readiness,
    IVerificationMailSink sink) : BackgroundService
{
    public override Task StartAsync(CancellationToken ct) { readiness.Running=readiness.Configured; return base.StartAsync(ct); }
    public override async Task StopAsync(CancellationToken ct) { readiness.Running=false; await base.StopAsync(ct); }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try {
            while(!stoppingToken.IsCancellationRequested) {
                if(readiness.Configured) {
                    try {
                        using var scope=scopes.CreateScope();
                        var db=scope.ServiceProvider.GetRequiredService<TiemChungDbContext>();
                        await readiness.RequireAsync(db,"probe@example.invalid",stoppingToken);
                        await scope.ServiceProvider.GetRequiredService<IdentityMailSink>().DrainAsync(sink,stoppingToken);
                    } catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { break; }
                    catch(Exception) { /* No payload/provider exception logging; retry next poll. */ }
                }
                await Task.Delay(TimeSpan.FromSeconds(1),stoppingToken);
            }
        } catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { }
        finally { readiness.Running=false; }
    }
}
