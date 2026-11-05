using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Contracts;
using TiemChung.Infrastructure.Data;
using Xunit;
using Fixture = TiemChung.Tests.AppointmentSqlTests.Fixture;

namespace TiemChung.Tests;

public sealed class EmployeeAuthorizationTests
{
    public static TheoryData<string, string[]> Matrix => new() {
        { "LeTan", ["XemTongQuan", "XemHoSo", "profiles.write", "registrations.manage", "QuanLyLichHen", "GuiNhacLich"] },
        { "BacSi", ["XemTongQuan", "XemHoSo", "screening.manage"] },
        { "DieuDuong", ["XemTongQuan", "XemHoSo", "vaccinations.record", "aftercare.record"] },
        { "ThuNgan", ["XemTongQuan", "XemThanhToan", "GhiThanhToan"] },
        { "ThuKho", ["XemTongQuan", "XemKho", "NhapKho"] },
        { "Admin", ["XemTongQuan", "QuanLyDanhMucChuyenMon", "QuanLyDanhMucGia", "QuanLyKhuyenMai", "XemBaoCao", "QuanLyTaiKhoan"] }
    };
    [Theory, MemberData(nameof(Matrix))]
    public void FixedRoleHasOnlyItsOwnPermissions(string role, string[] expected)
    {
        Assert.Equal(expected.Order(), EmployeeAuthorization.PermissionsFor(role).Order());
        var caps = EmployeeAuthorization.CapabilitiesFor(role);
        Assert.True(caps.Overview); Assert.True(caps.ChangePassword);
        Assert.Equal(role == "LeTan", caps.ProfilesCreate);
        Assert.Equal(role == "Admin", caps.Accounts);
        Assert.Empty(EmployeeAuthorization.PermissionsFor("Customer"));
        Assert.Empty(EmployeeAuthorization.PermissionsFor(null));
        Assert.False(EmployeeAuthorization.CapabilitiesFor("Admin", "Director").Accounts);
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task EveryRoleIsAuthorizedAtCriticalHttpRoutesAndMeIgnoresLegacyGrants()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var actions = new (string Method, string Path, string[] Roles, HttpStatusCode Allowed)[] {
                ("GET", "/api/v1/customers", ["LeTan", "BacSi", "DieuDuong"], HttpStatusCode.OK),
                ("POST", "/api/v1/customers", ["LeTan"], HttpStatusCode.BadRequest),
                ("GET", "/api/v1/registrations", ["LeTan"], HttpStatusCode.OK),
                ("POST", "/api/v1/registrations", ["LeTan"], HttpStatusCode.BadRequest),
                ("GET", "/api/v1/appointments", ["LeTan"], HttpStatusCode.OK),
                ("POST", "/api/v1/appointments", ["LeTan"], HttpStatusCode.BadRequest),
                ("GET", "/api/v1/clinical/appointments", ["BacSi"], HttpStatusCode.OK),
                ("POST", "/api/v1/clinical/appointments/2147483647/assessment", ["BacSi"], HttpStatusCode.BadRequest),
                ("GET", "/api/v1/vaccinations/eligible", ["DieuDuong"], HttpStatusCode.OK),
                ("POST", "/api/v1/vaccinations", ["DieuDuong"], HttpStatusCode.BadRequest),
                ("GET", "/api/v1/vaccinations/follow-up-pending", ["DieuDuong"], HttpStatusCode.OK),
                ("POST", "/api/v1/vaccinations/2147483647/follow-ups", ["DieuDuong"], HttpStatusCode.BadRequest),
                ("GET", "/api/v1/payment-registrations", ["ThuNgan"], HttpStatusCode.OK),
                ("POST", "/api/v1/registrations/2147483647/payment", ["ThuNgan"], HttpStatusCode.NotFound),
                ("GET", "/api/v1/inventory/lots", ["ThuKho"], HttpStatusCode.OK),
                ("POST", "/api/v1/inventory/receipts", ["ThuKho"], HttpStatusCode.BadRequest),
                ("POST", "/api/v1/catalog/vaccine-technologies", ["Admin"], HttpStatusCode.BadRequest),
                ("GET", "/api/v1/promotions/", ["Admin"], HttpStatusCode.OK),
                ("POST", "/api/v1/promotions/", ["Admin"], HttpStatusCode.BadRequest),
                ("GET", "/api/v1/reports/monthly-revenue", ["Admin"], HttpStatusCode.OK),
                ("GET", "/api/v1/employee-access", ["Admin"], HttpStatusCode.OK),
                ("PUT", "/api/v1/employee-access/2147483647/role", ["Admin"], HttpStatusCode.BadRequest)
            };
            foreach (var role in EmployeeAuthorization.Roles) {
                using var client = await f.Actor(role, false);
                // An adversarial legacy grant set must neither add nor remove role capabilities.
                foreach (var permission in await f.Db.Set<QuyenHan>().ToArrayAsync())
                    f.Db.Add(new TaiKhoan_QuyenHan { MaTaiKhoan=f.LastAccountId, MaQuyen=permission.MaQuyen });
                await f.Db.SaveChangesAsync();
                var before = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
                Assert.Equal(EmployeeAuthorization.PermissionsFor(role).Order(), before.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()!).Order());
                var expectedCaps = JsonSerializer.SerializeToElement(EmployeeAuthorization.CapabilitiesFor(role), new JsonSerializerOptions(JsonSerializerDefaults.Web));
                Assert.Equal(expectedCaps.ToString(), before.GetProperty("capabilities").ToString());
                await f.Db.Set<TaiKhoan_QuyenHan>().Where(g => g.MaTaiKhoan == f.LastAccountId).ExecuteDeleteAsync();
                var after = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
                Assert.Equal(before.ToString(), after.ToString());
                foreach (var action in actions) {
                    using var request = new HttpRequestMessage(new HttpMethod(action.Method), action.Path);
                    if (action.Method != "GET") {
                        object body = action.Path switch {
                            "/api/v1/registrations" => RegistrationSqlTests.SyntheticRequest(0, 0, []),
                            "/api/v1/promotions/" => new UpsertPromotionRequest("", "", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0, true),
                            _ => new { }
                        };
                        request.Content = JsonContent.Create(body);
                    }
                    using var response = await client.SendAsync(request);
                    var expected = action.Roles.Contains(role) ? action.Allowed : HttpStatusCode.Forbidden;
                    Assert.True(response.StatusCode == expected, $"{role} {action.Method} {action.Path}: expected {expected}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
                }
                var employeeId = (await f.Db.Set<TaiKhoan>().AsNoTracking().SingleAsync(a => a.MaTaiKhoan == f.LastAccountId)).MaNhanVien;
                await f.Db.Set<NhanVien>().Where(e => e.MaNhanVien == employeeId).ExecuteUpdateAsync(s => s.SetProperty(e => e.TrangThai, "Inactive"));
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
            }
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task A12PersistsRoleRevokesSessionsRejectsSelfStaleAndRollsBackAuditFailure()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            using var admin = await f.Actor("Admin", false); var adminId = f.LastAccountId;
            using var nurse = await f.Actor("DieuDuong", false); var id = f.LastAccountId;
            var fixtureAccount = await f.Db.Set<TaiKhoan>().SingleAsync(a => a.MaTaiKhoan == id);
            const string password = "synthetic-authz-r1-password";
            fixtureAccount.MatKhauHash = new Microsoft.AspNetCore.Identity.PasswordHasher<TaiKhoan>().HashPassword(fixtureAccount, password);
            await f.Db.SaveChangesAsync();
            var original = await f.Db.Set<TaiKhoan>().AsNoTracking().SingleAsync(a => a.MaTaiKhoan == id);
            object Body(string title, byte[]? version = null) => new { title, expectedTitle="DieuDuong", version=version ?? original.RowVersion };
            Assert.Equal(HttpStatusCode.Forbidden, (await nurse.PutAsJsonAsync($"/api/v1/employee-access/{id}/role", Body("Admin"))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync($"/api/v1/employee-access/{adminId}/role", Body("ThuKho"))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/v1/employee-access/{id}/role", Body("Custom"))).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/v1/employee-access/{id}/role", Body("ThuKho", new byte[8]))).StatusCode);
            await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER AuthzAuditFailure ON SecurityAudit AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE Action='employeeRoleChanged') THROW 51000, 'Synthetic failure', 1;");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await admin.PutAsJsonAsync($"/api/v1/employee-access/{id}/role", Body("ThuKho"))).StatusCode);
            Assert.Equal("DieuDuong", await f.Db.Set<NhanVien>().Where(e => e.MaNhanVien == original.MaNhanVien).Select(e => e.ChucDanh).SingleAsync());
            Assert.Equal(HttpStatusCode.OK, (await nurse.GetAsync("/api/v1/auth/me")).StatusCode);
            await f.Db.Database.ExecuteSqlRawAsync("DROP TRIGGER AuthzAuditFailure;");
            Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/v1/employee-access/{id}/role", Body("ThuKho"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await nurse.GetAsync("/api/v1/auth/me")).StatusCode);
            Assert.Equal("ThuKho", await f.Db.Set<NhanVien>().Where(e => e.MaNhanVien == original.MaNhanVien).Select(e => e.ChucDanh).SingleAsync());
            var saved = await f.Db.Set<TaiKhoan>().AsNoTracking().SingleAsync(a => a.MaTaiKhoan == id);
            Assert.Equal(original.SecurityVersion + 1, saved.SecurityVersion);
            Assert.Equal(original.MatKhauHash, saved.MatKhauHash);
            using var login = await nurse.PostAsJsonAsync("/api/v1/auth/login", new { identifier=original.TenDangNhap, password, clientType="native" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
            nurse.DefaultRequestHeaders.Authorization = new("Bearer", tokens.GetProperty("accessToken").GetString());
            var me = await nurse.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
            Assert.True(me.GetProperty("capabilities").GetProperty("inventoryReceive").GetBoolean());
            Assert.False(me.GetProperty("capabilities").GetProperty("vaccinations").GetBoolean());
            Assert.Equal(HttpStatusCode.OK, (await nurse.GetAsync("/api/v1/inventory/lots")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await nurse.GetAsync("/api/v1/vaccinations/eligible")).StatusCode);
            Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(a => a.Action == "employeeRoleChanged"));
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/v1/employee-access/{id}/role", Body("BacSi"))).StatusCode);
        });
    }
}
