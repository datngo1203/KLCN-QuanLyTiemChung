using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

// Creates one Development-only demo employee. Runtime permissions are derived
// from EmployeeAuthorization, not from seed-time SQL grants.
public static class DevelopmentDemoEmployeeSeed
{
    private static readonly string[] AllowedRoles = ["BacSi", "DieuDuong", "ThuNgan", "ThuKho"];

    public static async Task RunAsync(TiemChungDbContext db, IHostEnvironment environment,
        IConfiguration config, CancellationToken ct = default)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Demo employee seed is Development only.");

        var role = config["DevelopmentSeed:Role"] ?? "";
        var username = config["DevelopmentSeed:Username"] ?? "";
        var password = config["DevelopmentSeed:Password"] ?? "";
        if (!AllowedRoles.Contains(role) || username.Length is < 4 or > 100 || username != username.Trim() ||
            username.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('.' or '_' or '-')) ||
            password.Length is < 12 or > 128)
            throw new InvalidOperationException("Set valid DevelopmentSeed role, username and password.");

        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        await IdentityTransactions.AdministrationLockAsync(db, ct);
        var normalized = IdentityRegistration.Normalize(username);
        var existing = await db.Set<TaiKhoan>().SingleOrDefaultAsync(a => a.NormalizedUsername == normalized, ct);
        if (existing is not null) {
            if (existing.AccountType != "Employee" || !await db.Set<NhanVien>().AnyAsync(e =>
                e.MaNhanVien == existing.MaNhanVien && e.ChucDanh == role, ct))
                throw new InvalidOperationException("Seed username belongs to another role. Nothing changed.");
            return; // Never reset an existing account's password, status or employee data.
        }

        var names = new Dictionary<string, string>(StringComparer.Ordinal) {
            ["BacSi"] = "Bác sĩ", ["DieuDuong"] = "Điều dưỡng",
            ["ThuNgan"] = "Thu ngân", ["ThuKho"] = "Nhân viên kho"
        };
        var employee = new NhanVien {
            HoTen = names[role], CCCD = "", SoDienThoai = "", Email = "",
            BoPhan = "Development", ChucDanh = role, TrangThai = "Active"
        };
        db.Add(employee);
        await db.SaveChangesAsync(ct);
        var account = new TaiKhoan {
            MaNhanVien = employee.MaNhanVien, AccountType = "Employee",
            TenDangNhap = username, NormalizedUsername = normalized,
            TrangThai = "Active", AdministrativeDuty = "None", CreatedAt = DateTimeOffset.UtcNow
        };
        account.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(account, password);
        db.Add(account);
        await db.SaveChangesAsync(ct);
        IdentityTransactions.Audit(db, "developmentDemoEmployeeSeeded", "development-seed", subject: account.MaTaiKhoan);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
