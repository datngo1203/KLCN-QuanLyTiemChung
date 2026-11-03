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
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async fixture => {
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
            var created=await Created(client.PostAsJsonAsync(Path(route),new { name="Type "+route, description="demo", code=route == "injection-types" ? "LTC-TYPE" : "LVX-TYPE" }));
            types[route]=created.GetProperty("id").GetInt32();
            Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync(Path(route,types[route]),new { name="Updated "+route, description="" })).StatusCode);
            Assert.Equal("Updated "+route,(await List(client,route))[0].GetProperty("name").GetString());
            Assert.Equal(HttpStatusCode.NotFound,(await client.PutAsJsonAsync(Path(route,int.MaxValue),new { name="valid",description="" })).StatusCode);
        }
        var disease=(await Created(client.PostAsJsonAsync(Path("diseases"),new { code="BENH-SYNTHETIC", name="Synthetic disease", description="" }))).GetProperty("id").GetInt32();
        VaccineRequest Vaccine(string name="Vaccine", decimal price=125000, int? type=null) => new(name,"Demo maker","Demo origin",price,type ?? types["vaccine-types"],types["injection-types"],
            Code:name == "Second" ? "VX-SECOND" : "VX-VACCINE", DiseaseIds:[disease], TargetAudience:"Synthetic audience", AdministrationRoute:"Oral");
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
        PackageRequest Package(params PackageItemDto[] items) => new("Package",300000,items,Code:"GOI-PACKAGE",TargetAudience:"Synthetic audience");
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
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(Path("diseases",disease))).StatusCode);
        foreach (var route in new[] { "vaccines","packages","vaccine-types","injection-types","diseases" }) {
            Assert.Empty((await List(client,route)).EnumerateArray());
            Assert.Equal(HttpStatusCode.NotFound,(await client.DeleteAsync(Path(route,int.MaxValue))).StatusCode);
        }
        Assert.All(await fixture.Db.Set<SecurityAudit>().Where(x=>x.Action.StartsWith("catalog.")).ToArrayAsync(), x => {
            Assert.NotNull(x.ActorAccountId); Assert.NotNull(x.ResourceId); Assert.True(x.OccurredAt > DateTimeOffset.UtcNow.AddHours(-1));
        });
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task EveryWriteChecksCurrentEmployeePermissionsAndCustomerIsReadOnly()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        var routes=new[] { "injection-types","vaccine-types","diseases","vaccines","packages" };
        object Payload(string route) => route switch {
            "vaccines" => new VaccineRequest("Vaccine","Maker","Origin",1,1,1),
            "packages" => new PackageRequest("Package",1,[new(1,1)]),
            _ => new CatalogTypeRequest("Type","",route == "injection-types" ? "LTC-TYPE" : route == "diseases" ? "BENH-TYPE" : "LVX-TYPE")
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
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task SeedIsDevelopmentOnlyRepeatableAndWindowsServiceWritesActualSql()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
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
        await desktop.SaveAsync("injection-types",null,new { name="Windows SQL",description="",code="LTC-WINDOWS-SQL" });
        var created=(await desktop.ListAsync<TiemChung.Desktop.Services.CatalogType>("injection-types","Windows SQL")).Single();
        await desktop.SaveAsync("injection-types",created.Id,new { name="Windows updated",description="saved" });
        Assert.Equal("Windows updated",(await f.Db.Set<LoaiTiemChung>().AsNoTracking().SingleAsync(x=>x.MaLoaiTiemChung==created.Id)).TenLoaiTiemChung);
        await Assert.ThrowsAsync<HttpRequestException>(()=>desktop.DeleteAsync("vaccines",rows[0].Id));
        await desktop.DeleteAsync("injection-types",created.Id);
        Assert.Empty(await desktop.ListAsync<TiemChung.Desktop.Services.CatalogType>("injection-types","Windows"));
        await identity.LogoutAsync();
        await Assert.ThrowsAsync<HttpRequestException>(()=>desktop.SaveAsync("injection-types",null,new { name="Denied",description="" }));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task WindowsCommandsManageAllFourCatalogsWithRF02ACompatibilityFields()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
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
            if (vm.ShowPrice) vm.TargetAudience="Synthetic audience";
            if (vm.ShowVaccine) { vm.Diseases[0].Included=true; vm.AdministrationRoute=vm.AdministrationRoutes.First(); }
            var code=vm.Code; Assert.False(vm.CodeReadOnly); Assert.NotEmpty(code);
            await Command(vm.SaveCommand); Assert.Contains("Đã tải/lưu",vm.Feedback);
            vm.Search="Windows command "+tab; await Command(vm.LoadCommand); Assert.Single(vm.Rows);
            vm.Selected=vm.Rows.Single(); var id=vm.Selected.Id;
            Assert.True(vm.CodeReadOnly); Assert.Equal(code,vm.Code);
            vm.Code="VX-ILLEGAL-EDIT"; Assert.Equal(code,vm.Code);
            vm.Name="Windows updated "+tab; Assert.Equal(code,vm.Code); await Command(vm.SaveCommand); Assert.Contains("Đã tải/lưu",vm.Feedback);
            vm.Search="Windows updated "+tab; await Command(vm.LoadCommand); vm.Selected=Assert.Single(vm.Rows); Assert.Equal(id,vm.Selected.Id);
            if (vm.ShowPackage) Assert.Equal("2",vm.PackageChoices[1].Quantity);
            await Command(vm.DeleteCommand); Assert.Contains("Đã tải/lưu",vm.Feedback); Assert.Empty(vm.Rows);
            vm.Search="";
        }
        await Command(vm.SelectTabCommand,"Vắc xin"); vm.Selected=vm.Rows.First();
        await Command(vm.DeleteCommand); Assert.Contains("đang được tham chiếu",vm.Feedback); Assert.Equal(2,vm.Rows.Count);
        await identity.LogoutAsync();
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task WindowsCreateValidatesActiveProfileAndPreservesDraftCodeAndRealInactiveStatus()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            await DevelopmentCatalogSeed.RunAsync(f.Db,new SeedEnvironment("Development"));
            using var http=f.Factory.CreateClient(); var identity=new TiemChung.Desktop.Services.IdentityApiService(http);
            Assert.True((await identity.LoginAsync("s2.seed",f.Password)).Succeeded);
            var vm=new TiemChung.Desktop.ViewModels.CatalogViewModel(new(identity),identity.Permissions);
            await vm.InitializeAsync();Assert.Equal(vm.VaccineTypes[0].Name,vm.VaccineTypes[0].ToString());
            async Task Command(TiemChung.Desktop.ViewModels.RelayCommand command,object? parameter=null) {
                Assert.True(command.CanExecute(parameter));command.Execute(parameter);
                using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
                while(!vm.CanInteract) await Task.Delay(10,timeout.Token);
            }
            var notified=new List<string?>();vm.PropertyChanged+=(_,e)=>notified.Add(e.PropertyName);
            await Command(vm.NewCommand);notified.Clear();vm.Name="[DEMO] Vắc xin Đỏ";
            Assert.Equal("VX-DEMO-VAC-XIN-DO",vm.Code);Assert.Contains(nameof(vm.Code),notified);
            vm.Code="VX-WINDOWS-CUSTOM";vm.Name="[DEMO] Tên tự chọn";Assert.Equal("VX-WINDOWS-CUSTOM",vm.Code);
            vm.Manufacturer="Synthetic";vm.Origin="Synthetic";vm.Price="100000";vm.VaccineType=vm.VaccineTypes.First();vm.InjectionType=vm.InjectionTypes.First();
            foreach(var missing in new[]{"target","route","disease"}) {
                vm.TargetAudience=missing=="target" ? "" : "Synthetic audience";
                vm.AdministrationRoute=missing=="route" ? null : vm.AdministrationRoutes.First();
                vm.Diseases[0].Included=missing!="disease";
                await Command(vm.SaveCommand);Assert.Contains("Kiểm tra",vm.Feedback);
                Assert.Equal("VX-WINDOWS-CUSTOM",vm.Code);Assert.Equal("[DEMO] Tên tự chọn",vm.Name);
                Assert.Equal(2,await f.Db.Vaccines.CountAsync());
            }
            vm.TargetAudience="Synthetic audience";vm.AdministrationRoute="Other";vm.Diseases[0].Included=true;
            await Command(vm.SaveCommand);Assert.Contains("Kiểm tra",vm.Feedback);
            vm.AdministrationRouteNote="Synthetic route";
            await Command(vm.SaveCommand);Assert.Contains("Đã tải/lưu",vm.Feedback);
            var saved=await f.Db.Vaccines.AsNoTracking().SingleAsync(x=>x.Code=="VX-WINDOWS-CUSTOM");Assert.Equal("Active",saved.TrangThai);
            Assert.Equal("Synthetic audience",saved.DoiTuongSuDung);Assert.Equal("Other",saved.DuongDung);
            Assert.Single(await f.Db.Set<VacXin_Benh>().Where(x=>x.MaVacXin==saved.MaVacXin).ToArrayAsync());
            await f.Db.Vaccines.Where(x=>x.MaVacXin==saved.MaVacXin).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Inactive"));
            await Command(vm.LoadCommand);vm.Selected=vm.Rows.Single(x=>x.Id==saved.MaVacXin);
            Assert.Equal("Inactive — Không đang sử dụng",vm.StatusLabel);Assert.True(vm.CodeReadOnly);
            vm.Code="VX-REJECTED";vm.Name="Synthetic inactive rename";Assert.Equal(saved.Code,vm.Code);
            await Command(vm.SaveCommand);Assert.Contains("Đã tải/lưu",vm.Feedback);
            var inactive=await f.Db.Vaccines.AsNoTracking().SingleAsync(x=>x.MaVacXin==saved.MaVacXin);
            Assert.Equal("Inactive",inactive.TrangThai);Assert.Equal(saved.Code,inactive.Code);Assert.Equal("Synthetic audience",inactive.DoiTuongSuDung);
            await Command(vm.SelectTabCommand,"Gói vắc xin");vm.Name="[DEMO] Windows package";vm.Price="100000";vm.PackageChoices[0].Included=true;
            var packageCode=vm.Code;await Command(vm.SaveCommand);Assert.Contains("Kiểm tra",vm.Feedback);Assert.Equal(packageCode,vm.Code);
            vm.TargetAudience="Synthetic package audience";await Command(vm.SaveCommand);Assert.Contains("Đã tải/lưu",vm.Feedback);
            var package=await f.Db.Set<GoiVacXin>().SingleAsync(x=>x.Code==packageCode);Assert.Equal("Active",package.TrangThai);Assert.Equal(vm.Rows.Single(x=>x.Id==package.MaGoi).Code,packageCode);
            await identity.LogoutAsync();
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task WindowsLegacyNamePriceEditsPreserveMissingMetadataAndCode()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            await DevelopmentCatalogSeed.RunAsync(f.Db,new SeedEnvironment("Development"));
            var vaccine=await f.Db.Vaccines.AsNoTracking().OrderBy(x=>x.MaVacXin).FirstAsync();
            var package=await f.Db.Set<GoiVacXin>().AsNoTracking().SingleAsync();
            await f.Db.Vaccines.Where(x=>x.MaVacXin==vaccine.MaVacXin).ExecuteUpdateAsync(s=>s
                .SetProperty(x=>x.DoiTuongSuDung,(string?)null).SetProperty(x=>x.DuongDung,(string?)null).SetProperty(x=>x.DuongDungKhac,(string?)null));
            await f.Db.Set<VacXin_Benh>().Where(x=>x.MaVacXin==vaccine.MaVacXin).ExecuteDeleteAsync();
            await f.Db.Set<GoiVacXin>().Where(x=>x.MaGoi==package.MaGoi).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.DoiTuongSuDung,(string?)null));
            using var http=f.Factory.CreateClient();var identity=new TiemChung.Desktop.Services.IdentityApiService(http);
            Assert.True((await identity.LoginAsync("s2.seed",f.Password)).Succeeded);
            var vm=new TiemChung.Desktop.ViewModels.CatalogViewModel(new(identity),identity.Permissions);await vm.InitializeAsync();
            async Task Command(TiemChung.Desktop.ViewModels.RelayCommand command,object? parameter=null) {
                Assert.True(command.CanExecute(parameter));command.Execute(parameter);
                using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
                while(!vm.CanInteract) await Task.Delay(10,timeout.Token);
            }
            vm.Selected=vm.Rows.Single(x=>x.Id==vaccine.MaVacXin);vm.Name="[DEMO] Legacy renamed";vm.Price="234000";
            await Command(vm.SaveCommand);Assert.Contains("Đã tải/lưu",vm.Feedback);
            var saved=await f.Db.Vaccines.AsNoTracking().SingleAsync(x=>x.MaVacXin==vaccine.MaVacXin);
            Assert.Equal("[DEMO] Legacy renamed",saved.TenVacXin);Assert.Equal(234000,saved.Gia);Assert.Equal(vaccine.Code,saved.Code);
            Assert.Null(saved.DoiTuongSuDung);Assert.Null(saved.DuongDung);Assert.Null(saved.DuongDungKhac);
            Assert.Empty(await f.Db.Set<VacXin_Benh>().Where(x=>x.MaVacXin==saved.MaVacXin).ToArrayAsync());
            await Command(vm.SelectTabCommand,"Gói vắc xin");vm.Selected=vm.Rows.Single(x=>x.Id==package.MaGoi);
            vm.Name="[DEMO] Legacy package renamed";vm.Price="345000";await Command(vm.SaveCommand);Assert.Contains("Đã tải/lưu",vm.Feedback);
            var savedPackage=await f.Db.Set<GoiVacXin>().AsNoTracking().SingleAsync(x=>x.MaGoi==package.MaGoi);
            Assert.Equal("[DEMO] Legacy package renamed",savedPackage.TenGoi);Assert.Equal(345000,savedPackage.GiaGoi);Assert.Equal(package.Code,savedPackage.Code);Assert.Null(savedPackage.DoiTuongSuDung);
            // Supplying new metadata on a legacy record still writes and validates the actual values.
            await Command(vm.SelectTabCommand,"Vắc xin");vm.Selected=vm.Rows.Single(x=>x.Id==vaccine.MaVacXin);
            vm.TargetAudience="[DEMO] Newly supplied";vm.Diseases[0].Included=true;vm.AdministrationRoute=vm.AdministrationRoutes.First();
            await Command(vm.SaveCommand);Assert.Contains("Đã tải/lưu",vm.Feedback);
            var enriched=await f.Db.Vaccines.AsNoTracking().SingleAsync(x=>x.MaVacXin==vaccine.MaVacXin);
            Assert.Equal("[DEMO] Newly supplied",enriched.DoiTuongSuDung);Assert.Equal(vm.AdministrationRoutes.First(),enriched.DuongDung);
            Assert.Single(await f.Db.Set<VacXin_Benh>().Where(x=>x.MaVacXin==vaccine.MaVacXin).ToArrayAsync());
            await identity.LogoutAsync();
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task WindowsExplicitClearingExistingMetadataIsRejectedAndKeepsTheEdit()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            await DevelopmentCatalogSeed.RunAsync(f.Db,new SeedEnvironment("Development"));
            using var http=f.Factory.CreateClient();var identity=new TiemChung.Desktop.Services.IdentityApiService(http);
            Assert.True((await identity.LoginAsync("s2.seed",f.Password)).Succeeded);
            var vm=new TiemChung.Desktop.ViewModels.CatalogViewModel(new(identity),identity.Permissions);await vm.InitializeAsync();
            async Task Save() {
                vm.SaveCommand.Execute(null);using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
                while(!vm.CanInteract) await Task.Delay(10,timeout.Token);
                Assert.Contains("Kiểm tra",vm.Feedback);
            }
            var original=vm.Rows.First();
            foreach(var field in new[]{"target","disease","route"}) {
                vm.Selected=original;vm.Name="[DEMO] Must not persist";
                if(field=="target") vm.TargetAudience="";
                if(field=="disease") foreach(var choice in vm.Diseases) choice.Included=false;
                if(field=="route") vm.AdministrationRoute=null;
                await Save();Assert.Equal("[DEMO] Must not persist",vm.Name);Assert.Equal(original.Code,vm.Code);
                var unchanged=await f.Db.Vaccines.AsNoTracking().SingleAsync(x=>x.MaVacXin==original.Id);
                Assert.Equal(original.Name,unchanged.TenVacXin);Assert.Equal(original.Vaccine!.TargetAudience,unchanged.DoiTuongSuDung);
                Assert.Equal(original.Vaccine.AdministrationRoute,unchanged.DuongDung);
                Assert.Equal(original.Vaccine.Diseases!.Length,await f.Db.Set<VacXin_Benh>().CountAsync(x=>x.MaVacXin==original.Id));
            }
            vm.SelectTabCommand.Execute("Gói vắc xin");using(var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                while(!vm.CanInteract) await Task.Delay(10,timeout.Token);
            var package=vm.Rows.Single();vm.Selected=package;vm.TargetAudience="";vm.Name="[DEMO] Must not persist";
            await Save();var unchangedPackage=await f.Db.Set<GoiVacXin>().AsNoTracking().SingleAsync(x=>x.MaGoi==package.Id);
            Assert.Equal(package.Name,unchangedPackage.TenGoi);Assert.Equal(package.Package!.TargetAudience,unchangedPackage.DoiTuongSuDung);
            await identity.LogoutAsync();
        });
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
        internal required OwnedSqlTestDatabase Database { get; init; }
        public static async Task<Fixture> Create(Func<TiemChungDbContext, Task>? afterDatabaseCreated = null) {
            var owner = await OwnedSqlTestDatabase.CreateAsync(Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")
                ?? throw new InvalidOperationException("SQL required."));
            TiemChungDbContext? db = null;
            WebApplicationFactory<Program>? factory = null;
            HttpClient? client = null;
            try {
                var connection = owner.ConnectionString;
                var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
                    ["ConnectionStrings:TiemChung"]=connection,["Identity:MailMode"]="Disabled",
                    ["Identity:SigningKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
                    ["DevelopmentSeed:Username"]="s2.seed",["DevelopmentSeed:Password"]=Guid.NewGuid().ToString("N")
                }).Build();
                db = new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(connection).Options);
                if (afterDatabaseCreated is not null) await afterDatabaseCreated(db);
                await db.Database.MigrateAsync();
                await DevelopmentAdminSeed.RunAsync(db,new SeedEnvironment("Development"),config);
                factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseEnvironment("Development").ConfigureAppConfiguration((_,c)=>c.AddConfiguration(config)).ConfigureServices(s=> {
                    s.RemoveAll<DbContextOptions<TiemChungDbContext>>(); s.RemoveAll<TiemChungDbContext>(); s.AddDbContext<TiemChungDbContext>(o=>o.UseSqlServer(connection));
                }));
                client = factory.CreateClient();
                return new Fixture { Database=owner, Db=db,Factory=factory,Client=client,Config=config,Password=config["DevelopmentSeed:Password"]! };
            } catch (Exception failure) {
                await SqlFixtureLifecycle.CleanupAfterFailureAsync(failure, () => SqlFixtureLifecycle.CleanupAsync(
                    () => { client?.Dispose(); return ValueTask.CompletedTask; },
                    () => factory?.DisposeAsync() ?? ValueTask.CompletedTask,
                    () => db?.DisposeAsync() ?? ValueTask.CompletedTask,
                    owner.DisposeAsync));
                throw;
            }
        }
        public ValueTask DisposeAsync() => SqlFixtureLifecycle.CleanupAsync(
            () => { Client.Dispose(); return ValueTask.CompletedTask; },
            Factory.DisposeAsync, Db.DisposeAsync, Database.DisposeAsync);
    }

    private sealed class SeedEnvironment(string name) : IHostEnvironment {
        public string EnvironmentName { get; set; }=name;
        public string ApplicationName { get; set; }="CatalogTests";
        public string ContentRootPath { get; set; }=AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; }=new NullFileProvider();
    }
}
