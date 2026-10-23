using System.Net;
using System.Net.Http.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TiemChung.Api;
using TiemChung.IdentityOperator;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.BoundaryAcceptance;

public sealed class AppHttpTests
{
    [Fact]
    public async Task AppCredentialRunsCustomerHttpWithoutOperatorSecret()
    {
        var cs=Environment.GetEnvironmentVariable("M1_BOUNDARY_APP_CONNECTION")??throw new InvalidOperationException("Provision first.");
        var values=new Dictionary<string,string?> { ["Identity:MailMode"]="Synthetic",
            ["Identity:SigningKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["Identity:MailEncryptionKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) };
        await using var host=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseEnvironment("Test")
            .UseSetting("ConnectionStrings:TiemChung",cs).UseSetting("Catalog:UseDemoData","false")
            .ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(values)).ConfigureServices(s=>{
                s.RemoveAll<DbContextOptions<TiemChungDbContext>>();s.RemoveAll<TiemChungDbContext>();
                s.AddDbContext<TiemChungDbContext>(o=>o.UseSqlServer(cs)); }));
        using var client=host.CreateClient();var email=Guid.NewGuid().ToString("N")+"@example.invalid";
        Assert.Equal(HttpStatusCode.Accepted,(await client.PostAsJsonAsync("/api/v1/auth/register",new {email,password="synthetic-passphrase"})).StatusCode);
        var sink=host.Services.GetRequiredService<SyntheticVerificationSink>();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while(!sink.Messages.Any(x=>x.Value.Address==email)) await Task.Delay(50,timeout.Token);
        var token=sink.Messages.Single(x=>x.Value.Address==email).Value.Token;
        Assert.Equal(HttpStatusCode.NoContent,(await client.PostAsJsonAsync("/api/v1/auth/email-verifications/confirm",new {token})).StatusCode);
        var login=await client.PostAsJsonAsync("/api/v1/auth/login",new {identifier=email,password="synthetic-passphrase",clientType="native"});
        login.EnsureSuccessStatusCode();
        var body=await login.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        client.DefaultRequestHeaders.Authorization=new("Bearer",body.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/v1/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/v1/recipients")).StatusCode);
        var refresh=await client.PostAsJsonAsync("/api/v1/auth/refresh",new {clientType="native",refreshToken=body.GetProperty("refreshToken").GetString()});
        refresh.EnsureSuccessStatusCode();body=await refresh.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(HttpStatusCode.NoContent,(await client.PostAsJsonAsync("/api/v1/auth/logout",new {clientType="native",refreshToken=body.GetProperty("refreshToken").GetString()})).StatusCode);
    }

    [Fact]
    public async Task ActualCliRejectsAppSqlPrincipalBeforePrompts()
    {
        var cs=Environment.GetEnvironmentVariable("M1_BOUNDARY_APP_CONNECTION")??throw new InvalidOperationException("Provision first.");
        var start=new ProcessStartInfo("dotnet") { RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false };
        start.ArgumentList.Add(typeof(IdentityAdministration).Assembly.Location);start.ArgumentList.Add("bootstrap-admin");
        start.Environment["M1_OPERATOR_CONNECTION"]=cs;
        start.Environment["M1_OPERATOR_ALLOWED_IDENTITIES"]="caller-invents-allowlist";
        start.Environment["M1_OPERATOR_SECRET"]="caller-invents-secret";
        using var process=Process.Start(start)!;process.StandardInput.Close();
        var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));await process.WaitForExitAsync(timeout.Token);
        Assert.Equal(1,process.ExitCode);var text=await output+await error;
        Assert.DoesNotContain("Existing employee ID",text);Assert.DoesNotContain(cs,text);Assert.DoesNotContain("caller-invents-secret",text);
    }
}
