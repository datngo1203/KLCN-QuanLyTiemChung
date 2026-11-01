using Microsoft.EntityFrameworkCore;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IdentityTokens>();
builder.Services.AddScoped<IdentityAuthorization>();
builder.Services.AddScoped<IdentityRegistration>();
builder.Services.AddScoped<IdentitySessions>();
builder.Services.AddSingleton<IdentityRateLimits>();
builder.Services.AddSingleton<IdentityMailReadiness>();
builder.Services.AddSingleton<SyntheticVerificationSink>();
builder.Services.AddSingleton<IVerificationMailSink>(s=>s.GetRequiredService<SyntheticVerificationSink>());
builder.Services.AddScoped<IdentityMailSink>();
builder.Services.AddHostedService<IdentityMailDispatcher>();
builder.Services.AddDbContext<TiemChungDbContext>((services, options) =>
    options.UseSqlServer(services.GetRequiredService<IConfiguration>().GetConnectionString("TiemChung")
        ?? "Server=localhost;Database=TiemChung;Integrated Security=True;TrustServerCertificate=True"));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
// Validate the completed configuration, including host/test overrides.
var demo = app.Configuration.GetValue<bool>("Catalog:UseDemoData");
if (demo && !app.Environment.IsDevelopment())
    throw new InvalidOperationException("Dữ liệu mẫu chỉ được dùng ở Development.");
if (!demo && string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("TiemChung")))
    throw new InvalidOperationException("Cần cấu hình ConnectionStrings:TiemChung khi tắt dữ liệu mẫu.");
if (args.Contains("--seed-inventory"))
{
    if (args.Any(x => x is "--seed-admin" or "--seed-reception" or "--seed-catalog" or "--migrate"))
        throw new InvalidOperationException("Run --seed-inventory separately from other database commands.");
    using var scope = app.Services.CreateScope();
    var added = await DevelopmentInventorySeed.RunAsync(scope.ServiceProvider.GetRequiredService<TiemChungDbContext>(), app.Environment);
    Console.WriteLine($"Inventory seed completed: {added} synthetic [DEMO] lots added; existing lots unchanged.");
    return;
}
if (args.Contains("--seed-admin"))
{
    using var scope = app.Services.CreateScope();
    await DevelopmentAdminSeed.RunAsync(scope.ServiceProvider.GetRequiredService<TiemChungDbContext>(), app.Environment, app.Configuration);
    return;
}
if (args.Contains("--seed-reception"))
{
    using var scope = app.Services.CreateScope();
    await DevelopmentReceptionSeed.RunAsync(scope.ServiceProvider.GetRequiredService<TiemChungDbContext>(), app.Environment, app.Configuration);
    return;
}
if (args.Contains("--migrate"))
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Migration command is Development only.");
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<TiemChungDbContext>().Database.MigrateAsync();
    return;
}
if (args.Contains("--seed-catalog"))
{
    using var scope = app.Services.CreateScope();
    await DevelopmentCatalogSeed.RunAsync(scope.ServiceProvider.GetRequiredService<TiemChungDbContext>(), app.Environment);
    return;
}
app.UseExceptionHandler();
app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/api/v1/vaccinations")) context.Response.Headers.CacheControl = "no-store";
    if (context.Request.Path.StartsWithSegments("/api/v1/clinical")) context.Response.Headers.CacheControl = "no-store";
    if (context.Request.Path.StartsWithSegments("/api/v1/appointments")) context.Response.Headers.CacheControl = "no-store";
    if (context.Request.Path.StartsWithSegments("/api/v1/auth")) context.Response.Headers.CacheControl = "no-store";
    if (context.Request.Path.StartsWithSegments("/api/v1/payment-registrations") ||
        context.Request.Path.StartsWithSegments("/api/v1/registrations") && context.Request.Path.Value!.EndsWith("/payment", StringComparison.OrdinalIgnoreCase))
        context.Response.Headers.CacheControl = "no-store";
    try { await next(context); }
    catch (IdentityFailure failure) {
        if (failure.Code == "refreshInProgress") context.Response.Headers.RetryAfter = "1";
        if (failure.Code == "rateLimited") context.Response.Headers.RetryAfter = "60";
        context.Response.StatusCode = failure.Status;
        await context.Response.WriteAsJsonAsync(new { type="about:blank", title=failure.Code, status=failure.Status,
            code=failure.Code, traceId=context.TraceIdentifier, errors=failure.Errors });
    }
    catch (BadHttpRequestException failure) when (context.Request.Path.StartsWithSegments("/api/v1/catalog") ||
        context.Request.Path.StartsWithSegments("/api/v1/registrations") ||
        context.Request.Path.StartsWithSegments("/api/v1/payment-registrations") ||
        context.Request.Path.StartsWithSegments("/api/v1/appointments") ||
        context.Request.Path.StartsWithSegments("/api/v1/clinical") ||
        context.Request.Path.StartsWithSegments("/api/v1/vaccinations")) {
        context.Response.StatusCode = failure.StatusCode;
        var code = failure.StatusCode == 400 ? "validation" : "invalidRequest";
        await context.Response.WriteAsJsonAsync(new { type="about:blank", title=code, status=failure.StatusCode,
            code, traceId=context.TraceIdentifier });
    }
    catch (Microsoft.Data.SqlClient.SqlException) {
        context.Response.StatusCode=503;
        await context.Response.WriteAsJsonAsync(new { type="about:blank",title="dependencyUnavailable",status=503,
            code="dependencyUnavailable",traceId=context.TraceIdentifier });
    }
});
app.UseCors();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "TiemChung.Api" }))
    .WithName("Health").WithDescription("Liveness: API chạy; không xác nhận SQL Server sẵn sàng.");
app.MapCatalogEndpoints();

app.MapGet("/api/v1/modules", () => new ModuleDto[] {
    new("reception", "Hồ sơ, đăng ký, thanh toán, ưu đãi", "Bảo", "in_progress"),
    new("followup", "Lịch, lịch sử, nhắc lịch, theo dõi", "Quý", "planned"),
    new("operations", "Danh mục, kho, khám, tài khoản", "Đạt", "planned")
}).WithName("ListModules");

app.MapProfileEndpoints();
app.MapIdentityEndpoints();
app.MapRegistrationEndpoints();
app.MapPaymentEndpoints();
app.MapAppointmentEndpoints();
app.MapReminderEndpoints();
app.MapClinicalEndpoints();
app.MapVaccinationEndpoints();
app.MapVaccinationHistoryEndpoints();
app.MapAftercareEndpoints();

app.Run();

public partial class Program { }
