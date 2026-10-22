using Microsoft.EntityFrameworkCore;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
var demo = builder.Configuration.GetValue<bool>("Catalog:UseDemoData");
var connection = builder.Configuration.GetConnectionString("TiemChung");
if (demo && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Dữ liệu mẫu chỉ được dùng ở Development.");
if (!demo && string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("Cần cấu hình ConnectionStrings:TiemChung khi tắt dữ liệu mẫu.");
builder.Services.AddDbContext<TiemChungDbContext>(options =>
    options.UseSqlServer(connection ?? "Server=localhost;Database=TiemChung;Integrated Security=True;TrustServerCertificate=True"));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "TiemChung.Api" }))
    .WithName("Health").WithDescription("Liveness: API chạy; không xác nhận SQL Server sẵn sàng.");
app.MapGet("/api/v1/catalog/vaccines", async (string? search, TiemChungDbContext db, CancellationToken ct) =>
{
    if (demo)
    {
        VaccineDto[] samples = [new(1, "Vắc xin mẫu A", "Nhà sản xuất mẫu", "Nguồn gốc mẫu", 450000m),
            new(2, "Vắc xin mẫu B", "Nhà sản xuất mẫu", "Nguồn gốc mẫu", 620000m)];
        return Results.Ok(new VaccineListResponse("demo", samples.Where(v =>
            string.IsNullOrWhiteSpace(search) || v.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToArray()));
    }
    var query = db.Vaccines.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(search)) query = query.Where(v => v.TenVacXin.Contains(search));
    var items = await query.OrderBy(v => v.MaVacXin).Take(100)
        .Select(v => new VaccineDto(v.MaVacXin, v.TenVacXin, v.NhaSanXuat, v.XuatXu, v.Gia)).ToListAsync(ct);
    return Results.Ok(new VaccineListResponse("database", items));
}).WithName("ListVaccines").WithDescription("Danh mục công khai; tối đa 100 mục. Dữ liệu Development là mẫu.");

app.MapGet("/api/v1/modules", () => new ModuleDto[] {
    new("reception", "Hồ sơ, đăng ký, thanh toán, ưu đãi", "Bảo", "in_progress"),
    new("followup", "Lịch, lịch sử, nhắc lịch, theo dõi", "Quý", "planned"),
    new("operations", "Danh mục, kho, khám, tài khoản", "Đạt", "planned")
}).WithName("ListModules");

app.MapProfileEndpoints();

app.Run();

public partial class Program { }