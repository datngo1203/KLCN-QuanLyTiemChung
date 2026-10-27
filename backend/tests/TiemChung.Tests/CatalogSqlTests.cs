using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class CatalogSqlTests
{
    [Fact, Trait("Category", "SqlIntegration")]
    public async Task CrudValidationReferencesAndPublicFiltersPersistInSql()
    {
        await using var fixture = await Fixture.Create();
        var client = fixture.Client;
        var types = new Dictionary<string,int>();
        foreach (var route in new[] { "injection-types", "vaccine-types" }) {
            Assert.Empty((await List(client,route)).EnumerateArray());
            Assert.Equal(HttpStatusCode.Unauthorized,(await client.PostAsJsonAsync(Path(route),new { name="Type", description="" })).StatusCode);
        }
        await fixture.Login();
        foreach (var route in new[] { "injection-types", "vaccine-types" }) {
            foreach (var name in new[] { "", " ", new string('a',121) })
                Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(Path(route),new { name, description="" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(Path(route),new { name="valid", description=new string('a',501) })).StatusCode);
            var created=await Created(client.PostAsJsonAsync(Path(route),new { name="Type "+route, description="demo" }));
            types[route]=created.GetProperty("id").GetInt32();
            Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync(Path(route,types[route]),new { name="Updated "+route, description="" })).StatusCode);
            Assert.Equal("Updated "+route,(await List(client,route))[0].GetProperty("name").GetString());
            Assert.Equal(HttpStatusCode.NotFound,(await client.PutAsJsonAsync(Path(route,int.MaxValue),new { name="valid",description="" })).StatusCode);
        }
        VaccineRequest Vaccine(string name="Vaccine", decimal price=125000, int? type=null) => new(name,"Demo maker","Demo origin",price,type ?? types["vaccine-types"],types["injection-types"]);
        foreach (var bad in new[] { Vaccine(""), Vaccine(price:-1), Vaccine(price:1.5m), Vaccine(price:10000000000000000m), Vaccine(type:0), Vaccine(type:int.MaxValue), Vaccine(new string('a',151)), Vaccine() with { Manufacturer=null! } })
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(Path("vaccines"),bad)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(Path("vaccines"),new { name="Vaccine", manufacturer="Maker", origin="Origin", vaccineTypeId=types["vaccine-types"], injectionTypeId=types["injection-types"] })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(Path("packages"),new { name="Package", items=new[] { new { vaccineId=1,quantity=1 } } })).StatusCode);
        foreach (var payload in new[] { "{", "{\"name\":\"Vaccine\",\"price\":\"bad\"}" }) {
            using var invalid=await client.PostAsync(Path("vaccines"),new StringContent(payload,System.Text.Encoding.UTF8,"application/json"));
            Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);
            using var problem=JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
            Assert.Equal("validation",problem.RootElement.GetProperty("code").GetString());
        }
        var vaccine=(await Created(client.PostAsJsonAsync(Path("vaccines"),Vaccine()))).GetProperty("id").GetInt32();
        var second=(await Created(client.PostAsJsonAsync(Path("vaccines"),Vaccine("Second",250000)))).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync(Path("vaccines",vaccine),Vaccine("Updated vaccine",150000))).StatusCode);
        Assert.Single((await List(client,$"vaccines?search=Updated&vaccineTypeId={types["vaccine-types"]}&injectionTypeId={types["injection-types"]}&minPrice=150000&maxPrice=150000")).EnumerateArray());
        Assert.Empty((await List(client,"vaccines?minPrice=300000")).EnumerateArray());
        foreach (var query in new[] { "minPrice=-1", "minPrice=2&maxPrice=1", "vaccineTypeId=0", "search="+new string('a',151) })
            Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync(Path("vaccines?"+query))).StatusCode);
        PackageRequest Package(params PackageItemDto[] items) => new("Package",300000,items);
        foreach (var bad in new[] { Package(), Package(new PackageItemDto(vaccine,0)), Package(new PackageItemDto(int.MaxValue,1)), Package(new PackageItemDto(vaccine,1),new(vaccine,2)), Package(new PackageItemDto(vaccine,101)), Package(new PackageItemDto(vaccine,1)) with { Price=-1 }, Package(new PackageItemDto(vaccine,1)) with { Items=null! } })
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(Path("packages"),bad)).StatusCode);
        var package=(await Created(client.PostAsJsonAsync(Path("packages"),Package(new PackageItemDto(vaccine,1),new(second,2))))).GetProperty("id").GetInt32();
        Assert.Equal(2,(await List(client,"packages"))[0].GetProperty("items").GetArrayLength());
        Assert.Single((await List(client,$"packages?vaccineTypeId={types["vaccine-types"]}&minPrice=300000&maxPrice=300000")).EnumerateArray());
        Assert.Empty((await List(client,"packages?vaccineTypeId=2147483647")).EnumerateArray());
        Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync(Path("packages",package),Package(new PackageItemDto(vaccine,3)))).StatusCode);
        Assert.Equal(3,(await List(client,"packages"))[0].GetProperty("items")[0].GetProperty("quantity").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PutAsJsonAsync(Path("packages",package),Package(new PackageItemDto(second,0)))).StatusCode);
        Assert.Equal(3,(await List(client,"packages"))[0].GetProperty("items")[0].GetProperty("quantity").GetInt32());
        foreach (var route in types.Keys) Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync(Path(route,types[route]))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync(Path("vaccines",vaccine))).StatusCode);

        // A registration references the package: deleting its owned items must roll back as well.
        var customer=new KhachHang { HoTen="Synthetic S2", CCCD="000000000222", SoDienThoai="0900000000", Email="s2@example.invalid" };
        fixture.Db.Add(customer); await fixture.Db.SaveChangesAsync();
        var recipient=new NguoiDuocTiem { MaKhachHang=customer.MaKhachHang,HoTen="Synthetic",NgaySinh=new(2020,1,1),GioiTinh="Khác",QuanHeVoiKhachHang="Bản thân" };
        fixture.Db.Add(recipient); await fixture.Db.SaveChangesAsync();
        var employee=await fixture.Db.Set<NhanVien>().SingleAsync();
        var registration=new DangKyDichVu { MaKhachHang=customer.MaKhachHang,MaNguoiDuocTiem=recipient.MaNguoiDuocTiem,MaNhanVien=employee.MaNhanVien,NgayDangKy=DateTime.UtcNow,ThoiGianDuKien=DateTime.UtcNow,HinhThucTiem="Goi",TrangThai="Draft" };
        fixture.Db.Add(registration); await fixture.Db.SaveChangesAsync();
        var reference=new ChiTietGoiVacXin { MaDangKy=registration.MaDangKy,MaGoi=package,SoLuongGoi=1,GiaTaiThoiDiemDangKy=300000 };
        fixture.Db.Add(reference); await fixture.Db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync(Path("packages",package))).StatusCode);
        Assert.Single(await fixture.Db.Set<GoiVacXin_VacXin>().AsNoTracking().ToArrayAsync());
        fixture.Db.Remove(reference); await fixture.Db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(Path("packages",package))).StatusCode);
        foreach (var id in new[] { vaccine,second }) Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(Path("vaccines",id))).StatusCode);
        foreach (var route in types.Keys) Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(Path(route,types[route]))).StatusCode);
        foreach (var route in new[] { "vaccines","packages","vaccine-types","injection-types" }) {
            Assert.Empty((await List(client,route)).EnumerateArray());
            Assert.Equal(HttpStatusCode.NotFound,(await client.DeleteAsync(Path(route,int.MaxValue))).StatusCode);
        }
        Assert.All(await fixture.Db.Set<SecurityAudit>().Where(x=>x.Action.StartsWith("catalog.")).ToArrayAsync(), x => {
            Assert.NotNull(x.ActorAccountId); Assert.NotNull(x.ResourceId); Assert.True(x.OccurredAt > DateTimeOffset.UtcNow.AddHours(-1));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task EveryWriteChecksCurrentEmployeePermissionsAndCustomerIsReadOnly()
    {
        await using var f=await Fixture.Create();
        var routes=new[] { "injection-types","vaccine-types","vaccines","packages" };
        object Payload(string route) => route switch {
            "vaccines" => new VaccineRequest("Vaccine","Maker","Origin",1,1,1),
            "packages" => new PackageRequest("Package",1,[new(1,1)]),
            _ => new CatalogTypeRequest("Type","")
        };
        async Task Denied(HttpStatusCode expected) {
            foreach (var route in routes) {
                Assert.Equal(HttpStatusCode.OK,(await f.Client.GetAsync(Path(route))).StatusCode);
                Assert.Equal(expected,(await f.Client.PostAsJsonAsync(Path(route),Payload(route))).StatusCode);
                Assert.Equal(expected,(await f.Client.PutAsJsonAsync(Path(route,1),Payload(route))).StatusCode);
                Assert.Equal(expected,(await f.Client.DeleteAsync(Path(route,1))).StatusCode);
            }
        }
        await Denied(HttpStatusCode.Unauthorized);
        await f.Login();
        var account=await f.Db.Set<TaiKhoan>().SingleAsync();
        var grants=await f.Db.Set<TaiKhoan_QuyenHan>().ToArrayAsync(); f.Db.RemoveRange(grants); await f.Db.SaveChangesAsync();
        await Denied(HttpStatusCode.Forbidden);
        var clinical=await f.Db.Set<QuyenHan>().SingleAsync(x=>x.Code=="QuanLyDanhMucChuyenMon");
        f.Db.Add(new TaiKhoan_QuyenHan { MaTaiKhoan=account.MaTaiKhoan, MaQuyen=clinical.MaQuyen }); await f.Db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Created,(await f.Client.PostAsJsonAsync(Path("vaccine-types"),Payload("vaccine-types"))).StatusCode);
        foreach (var route in new[] { "vaccines","packages" }) Assert.Equal(HttpStatusCode.Forbidden,(await f.Client.PostAsJsonAsync(Path(route),Payload(route))).StatusCode);
        // Verified customer with even an explicit catalog grant must still be refused.
        var customer=new TaiKhoan { AccountType="Customer",TenDangNhap="s2-customer",LoginEmail="s2@example.invalid",NormalizedEmail="S2@EXAMPLE.INVALID",EmailVerifiedAt=DateTimeOffset.UtcNow,TrangThai="Active",CreatedAt=DateTimeOffset.UtcNow };
        f.Db.Add(customer); await f.Db.SaveChangesAsync();
        f.Db.Add(new TaiKhoan_QuyenHan { MaTaiKhoan=customer.MaTaiKhoan,MaQuyen=clinical.MaQuyen });
        var session=new IdentitySession { Id=Guid.NewGuid(),AccountId=customer.MaTaiKhoan,SecurityVersion=1,CreatedAt=DateTimeOffset.UtcNow,ExpiresAt=DateTimeOffset.UtcNow.AddHours(1) };
        f.Db.Add(session); await f.Db.SaveChangesAsync();
        f.Client.DefaultRequestHeaders.Authorization=new("Bearer",new IdentityTokens(f.Config).Issue(customer.MaTaiKhoan,session.Id,DateTimeOffset.UtcNow));
        await Denied(HttpStatusCode.Forbidden);
        session.RevokedAt=DateTimeOffset.UtcNow; await f.Db.SaveChangesAsync();
        await Denied(HttpStatusCode.Unauthorized);
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task SeedIsDevelopmentOnlyRepeatableAndWindowsServiceWritesActualSql()
    {
        await using var f=await Fixture.Create();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>DevelopmentCatalogSeed.RunAsync(f.Db,new SeedEnvironment("Production")));
        await DevelopmentCatalogSeed.RunAsync(f.Db,new SeedEnvironment("Development"));
        var first=await f.Db.Vaccines.FirstAsync(); first.Gia=123456; await f.Db.SaveChangesAsync();
        await DevelopmentCatalogSeed.RunAsync(f.Db,new SeedEnvironment("Development"));
        Assert.Equal(2,await f.Db.Vaccines.CountAsync()); Assert.Equal(2,await f.Db.Set<LoaiTiemChung>().CountAsync());
        Assert.Equal(2,await f.Db.Set<LoaiVacXin>().CountAsync()); Assert.Single(await f.Db.Set<GoiVacXin>().ToArrayAsync());
        Assert.Equal(2,await f.Db.Set<GoiVacXin_VacXin>().CountAsync());
        Assert.Equal(123456,(await f.Db.Vaccines.AsNoTracking().SingleAsync(x=>x.MaVacXin==first.MaVacXin)).Gia);
        using var http=f.Factory.CreateClient();
        var identity=new TiemChung.Desktop.Services.IdentityApiService(http);
        Assert.True((await identity.LoginAsync("s2.seed",f.Password)).Succeeded);
        var desktop=new TiemChung.Desktop.Services.CatalogApiService(identity);
        var rows=await desktop.ListAsync<TiemChung.Desktop.Services.CatalogVaccine>("vaccines"); Assert.Equal(2,rows.Length);
        await desktop.SaveAsync("injection-types",null,new { name="Windows SQL",description="" });
        var created=(await desktop.ListAsync<TiemChung.Desktop.Services.CatalogType>("injection-types","Windows SQL")).Single();
        await desktop.SaveAsync("injection-types",created.Id,new { name="Windows updated",description="saved" });
        Assert.Equal("Windows updated",(await f.Db.Set<LoaiTiemChung>().AsNoTracking().SingleAsync(x=>x.MaLoaiTiemChung==created.Id)).TenLoaiTiemChung);
        await Assert.ThrowsAsync<HttpRequestException>(()=>desktop.DeleteAsync("vaccines",rows[0].Id));
        await desktop.DeleteAsync("injection-types",created.Id);
        Assert.Empty(await desktop.ListAsync<TiemChung.Desktop.Services.CatalogType>("injection-types","Windows"));
        await identity.LogoutAsync();
        await Assert.ThrowsAsync<HttpRequestException>(()=>desktop.SaveAsync("injection-types",null,new { name="Denied",description="" }));
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task WindowsCommandsManageAllFourCatalogsWithoutSampleState()
    {
        await using var f=await Fixture.Create();
        await DevelopmentCatalogSeed.RunAsync(f.Db,new SeedEnvironment("Development"));
        using var http=f.Factory.CreateClient();
        var identity=new TiemChung.Desktop.Services.IdentityApiService(http);
        Assert.True((await identity.LoginAsync("s2.seed",f.Password)).Succeeded);
        var vm=new TiemChung.Desktop.ViewModels.CatalogViewModel(new(identity), identity.Permissions);
        await vm.InitializeAsync(); Assert.Equal(2,vm.Rows.Count);
        async Task Command(TiemChung.Desktop.ViewModels.RelayCommand command, object? parameter=null) {
            Assert.True(command.CanExecute(parameter)); command.Execute(parameter);
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (!vm.CanInteract) await Task.Delay(10,timeout.Token);
        }
        foreach (var tab in new[] { "Loại tiêm chủng", "Loại vắc xin", "Vắc xin", "Gói vắc xin" }) {
            await Command(vm.SelectTabCommand,tab);
            await Command(vm.NewCommand);
            vm.Name="Windows command "+tab; vm.Description="SQL";
            if (vm.ShowVaccine) {
                vm.Manufacturer="Demo"; vm.Origin="Demo"; vm.Price="-1";
                vm.VaccineType=vm.VaccineTypes.First(); vm.InjectionType=vm.InjectionTypes.First();
                await Command(vm.SaveCommand); Assert.Contains("Giá phải",vm.Feedback);
                Assert.Equal("Windows command "+tab,vm.Name);
            }
            if (vm.ShowPrice) vm.Price="100000";
            if (vm.ShowPackage) { vm.PackageChoices[0].Included=true; vm.PackageChoices[1].Included=true; vm.PackageChoices[1].Quantity="2"; }
            await Command(vm.SaveCommand); Assert.Contains("Đã tải/lưu",vm.Feedback);
            vm.Search="Windows command "+tab; await Command(vm.LoadCommand); Assert.Single(vm.Rows);
            vm.Selected=vm.Rows.Single(); var id=vm.Selected.Id;
            vm.Name="Windows updated "+tab; await Command(vm.SaveCommand); Assert.Contains("Đã tải/lưu",vm.Feedback);
            vm.Search="Windows updated "+tab; await Command(vm.LoadCommand); vm.Selected=Assert.Single(vm.Rows); Assert.Equal(id,vm.Selected.Id);
            if (vm.ShowPackage) Assert.Equal("2",vm.PackageChoices[1].Quantity);
            await Command(vm.DeleteCommand); Assert.Contains("Đã tải/lưu",vm.Feedback); Assert.Empty(vm.Rows);
            vm.Search="";
        }
        await Command(vm.SelectTabCommand,"Vắc xin"); vm.Selected=vm.Rows.First();
        await Command(vm.DeleteCommand); Assert.Contains("đang được tham chiếu",vm.Feedback); Assert.Equal(2,vm.Rows.Count);
        await identity.LogoutAsync();
    }

    private static string Path(string route, int? id=null)=>"/api/v1/catalog/"+route+(id.HasValue ? $"/{id}" : "");
    private static async Task<JsonElement> Created(Task<HttpResponseMessage> call) {
        using var response=await call;
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
    private static async Task<JsonElement> List(HttpClient client,string route) {
        using var response=await client.GetAsync(Path(route)); Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("database",json.RootElement.GetProperty("source").GetString());
        return json.RootElement.GetProperty("items").Clone();
    }
    internal sealed class Fixture : IAsyncDisposable {
        public required TiemChungDbContext Db { get; init; }
        public required WebApplicationFactory<Program> Factory { get; init; }
        public required IConfiguration Config { get; init; }
        public required HttpClient Client { get; init; }
        public required string Password { get; init; }
        public async Task Login() {
            using var response=await Client.PostAsJsonAsync("/api/v1/auth/login",new { identifier="s2.seed",password=Password,clientType="native" });
            Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Client.DefaultRequestHeaders.Authorization=new("Bearer",json.RootElement.GetProperty("accessToken").GetString());
        }
        public static async Task<Fixture> Create() {
            var connection=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION") ?? throw new InvalidOperationException("SQL required.")) { InitialCatalog="KLCN_M1_Test_"+Guid.NewGuid().ToString("N") }.ConnectionString;
            var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
                ["ConnectionStrings:TiemChung"]=connection,["Catalog:UseDemoData"]="false",["Identity:MailMode"]="Disabled",
                ["Identity:SigningKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
                ["DevelopmentSeed:Username"]="s2.seed",["DevelopmentSeed:Password"]=Guid.NewGuid().ToString("N")
            }).Build();
            var db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(connection).Options);
            await db.Database.MigrateAsync();
            await DevelopmentAdminSeed.RunAsync(db,new SeedEnvironment("Development"),config);
            var factory=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseEnvironment("Development").ConfigureAppConfiguration((_,c)=>c.AddConfiguration(config)).ConfigureServices(s=> {
                s.RemoveAll<DbContextOptions<TiemChungDbContext>>(); s.RemoveAll<TiemChungDbContext>(); s.AddDbContext<TiemChungDbContext>(o=>o.UseSqlServer(connection));
            }));
            return new Fixture { Db=db,Factory=factory,Client=factory.CreateClient(),Config=config,Password=config["DevelopmentSeed:Password"]! };
        }
        public async ValueTask DisposeAsync() { Client.Dispose(); await Factory.DisposeAsync(); await Db.DisposeAsync(); }
    }
    private sealed class SeedEnvironment(string name) : IHostEnvironment {
        public string EnvironmentName { get; set; }=name;
        public string ApplicationName { get; set; }="CatalogTests";
        public string ContentRootPath { get; set; }=AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; }=new NullFileProvider();
    }
}
