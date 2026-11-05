using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

// A12 edits the fixed title template only; provisioning/reset remain in the existing operator tool.
public static class EmployeeAccessEndpoints
{
    private static string RoleLabel(string title) => title switch {
        "Admin" => "Quản trị viên", "LeTan" => "Lễ tân", "BacSi" => "Bác sĩ", "DieuDuong" => "Điều dưỡng",
        "ThuNgan" => "Thu ngân", "ThuKho" => "Thủ kho", _ => "Chức danh không hợp lệ"
    };
    private static string PermissionLabel(string code) => code switch {
        "XemTongQuan" => "Tổng quan", "XemHoSo" => "Đọc hồ sơ", EmployeeAuthorization.ProfilesWrite => "Tiếp nhận hồ sơ",
        EmployeeAuthorization.Registrations => "Đăng ký dịch vụ", EmployeeAuthorization.Screening => "Khám sàng lọc",
        EmployeeAuthorization.Vaccination => "Ghi nhận mũi tiêm", EmployeeAuthorization.Aftercare => "Theo dõi sau tiêm",
        "XemThanhToan" or "GhiThanhToan" => "Thanh toán", "XemKho" => "Kho vắc xin", "NhapKho" => "Nhập kho",
        "QuanLyLichHen" or "GuiNhacLich" => "Lịch hẹn và nhắc lịch", "QuanLyDanhMucChuyenMon" or "QuanLyDanhMucGia" => "Danh mục",
        "QuanLyKhuyenMai" => "Quản lý ưu đãi", "XemBaoCao" => "Báo cáo", "QuanLyTaiKhoan" => "Nhân viên và tài khoản", _ => ""
    };
    public sealed record ChangeRole(string Title, string ExpectedTitle, byte[] Version);
    public static void MapEmployeeAccessEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/employee-access").WithTags("Employee access");
        group.AddEndpointFilter(async (invocation, next) => {
            invocation.HttpContext.Response.Headers.CacheControl = "no-store";
            try { return await next(invocation); }
            catch (DbUpdateException) { throw new IdentityFailure(503, "dependencyUnavailable"); }
            catch (Microsoft.Data.SqlClient.SqlException) { throw new IdentityFailure(503, "dependencyUnavailable"); }
        });
        group.MapGet("", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            (await auth.RequireAsync(context, false, ct)).RequirePermission("QuanLyTaiKhoan");
            var items = await (from account in db.Set<TaiKhoan>().AsNoTracking()
                join employee in db.Set<NhanVien>().AsNoTracking() on account.MaNhanVien equals employee.MaNhanVien
                where account.AccountType == "Employee" orderby employee.HoTen, account.MaTaiKhoan
                select new { accountId=account.MaTaiKhoan, employeeId=employee.MaNhanVien, fullName=employee.HoTen,
                    username=account.TenDangNhap, title=employee.ChucDanh, employeeStatus=employee.TrangThai,
                    accountStatus=account.TrangThai, version=account.RowVersion }).ToArrayAsync(ct);
            var roles = EmployeeAuthorization.Roles.Select(title => new { title, label = RoleLabel(title),
                access = string.Join(" · ", EmployeeAuthorization.PermissionsFor(title).Select(PermissionLabel).Distinct()) });
            return Results.Ok(new { items, roles });
        });
        group.MapPut("/{id:int}/role", async (int id, ChangeRole request, HttpContext context, IdentityAuthorization auth,
            TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            await IdentityTransactions.AdministrationLockAsync(db, ct);
            var actor = await auth.RequireAsync(context, true, ct);
            actor.RequirePermission("QuanLyTaiKhoan");
            if (actor.Account.MaTaiKhoan == id) throw new IdentityFailure(403, "permissionDenied");
            if (!EmployeeAuthorization.IsRole(request.Title) || request.Version?.Length != 8 ||
                !EmployeeAuthorization.IsRole(request.ExpectedTitle)) throw new IdentityFailure(400, "validation");
            var account = await IdentityTransactions.LockAccountAsync(db, id, ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            if (account.AccountType != "Employee") throw new IdentityFailure(400, "validation");
            var employee = await db.Set<NhanVien>().SingleAsync(e => e.MaNhanVien == account.MaNhanVien, ct);
            if (!account.RowVersion.SequenceEqual(request.Version) || employee.ChucDanh != request.ExpectedTitle)
                throw new IdentityFailure(409, "concurrencyConflict");
            if (employee.ChucDanh != request.Title) {
                employee.ChucDanh = request.Title;
                await IdentityTransactions.RevokeAccountAsync(db, account, "employeeRoleChanged", context.TraceIdentifier, actor.Account.MaTaiKhoan, ct);
                var audit = IdentityTransactions.Audit(db, "employeeRoleChanged", context.TraceIdentifier, actor.Account.MaTaiKhoan, id, actor.Session.Id);
                audit.ChangedFields = "ChucDanh,SecurityVersion";
                await db.SaveChangesAsync(ct);
            }
            await tx.CommitAsync(ct);
            return Results.NoContent();
        });
    }
}
