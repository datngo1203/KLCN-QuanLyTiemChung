using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TiemChung.Contracts;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class ApiTests
{
    private static WebApplicationFactory<Program> Create() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.UseEnvironment("Development").ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:TiemChung"]="Server=127.0.0.1,1;Database=Unavailable;User ID=synthetic;Password=Unused-Test!;Connect Timeout=1;TrustServerCertificate=True" })));

    [Fact]
    public async Task HealthRunsWithoutDatabase()
    {
        await using var factory = Create();
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task CatalogFailsWithoutSqlInsteadOfReturningSampleData()
    {
        await using var factory = Create();
        using var response = await factory.CreateClient().GetAsync("/api/v1/catalog/vaccines");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("dependencyUnavailable", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task WebsiteCanCallApiFromItsDevelopmentOrigin()
    {
        await using var factory = Create();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/catalog/vaccines");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        var response = await factory.CreateClient().SendAsync(request);
        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public void SqlServerModelCanGenerateSchemaWithoutConnecting()
    {
        using var context = new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelCheck;Integrated Security=True").Options);
        var script = context.Database.GenerateCreateScript();
        Assert.Contains("CREATE TABLE [DangKyDichVu]", script);
        Assert.Contains("CREATE TABLE [InventoryReceipt]", script);
        Assert.Contains("MatKhauHash", script);
        Assert.Contains("CREATE TABLE [CustomerPasswordReset]", script);
        Assert.Contains("CREATE TABLE [ReminderEmail]", script);
        Assert.Contains("CREATE TABLE [AppointmentNotification]", script);
        Assert.Equal(41, context.Model.GetEntityTypes().Count());
    }
}
