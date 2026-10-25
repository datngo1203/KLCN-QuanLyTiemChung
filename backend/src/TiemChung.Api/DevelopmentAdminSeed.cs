using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class DevelopmentAdminSeed
{
    public static async Task RunAsync(TiemChungDbContext db, IHostEnvironment environment, IConfiguration config, CancellationToken ct = default)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Admin seed is Development only.");
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        await IdentityTransactions.AdministrationLockAsync(db, ct);
        if (await (from a in db.Set<TaiKhoan>() join e in db.Set<NhanVien>() on a.MaNhanVien equals e.MaNhanVien
            where a.AccountType == "Employee" && e.ChucDanh == "Admin" select a).AnyAsync(ct)) return;
        var username = config["DevelopmentSeed:Username"] ?? "";
        var password = config["DevelopmentSeed:Password"] ?? "";
        if (username.Length is < 3 or > 100 || username != username.Trim() || username.Contains('@') || password.Length is < 12 or > 128)
            throw new InvalidOperationException("Set valid DevelopmentSeed username/password in local environment.");
        var normalized = IdentityRegistration.Normalize(username);
        if (await db.Set<TaiKhoan>().AnyAsync(x => x.NormalizedUsername == normalized, ct))
            throw new InvalidOperationException("Seed username already exists; no account was changed.");
        var employee = new NhanVien { HoTen = "Admin development", CCCD = "", SoDienThoai = "", Email = "",
            BoPhan = "Development", ChucDanh = "Admin", TrangThai = "Active" };
        db.Add(employee);
        await db.SaveChangesAsync(ct);
        var account = new TaiKhoan { MaNhanVien = employee.MaNhanVien, AccountType = "Employee", TenDangNhap = username,
            NormalizedUsername = normalized, TrangThai = "Active", AdministrativeDuty = "Technical", CreatedAt = DateTimeOffset.UtcNow };
        account.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(account, password);
        db.Add(account);
        var permissions = await db.Set<QuyenHan>().ToListAsync(ct);
        foreach (var code in IdentityPermissions.Codes.Where(code => permissions.All(p => p.Code != code))) {
            var permission = new QuyenHan { Code = code, TenQuyen = code, MoTa = "Development seed" };
            db.Add(permission); permissions.Add(permission);
        }
        await db.SaveChangesAsync(ct);
        foreach (var permission in permissions.Where(p => IdentityPermissions.Codes.Contains(p.Code)))
            db.Add(new TaiKhoan_QuyenHan { MaTaiKhoan = account.MaTaiKhoan, MaQuyen = permission.MaQuyen });
        IdentityTransactions.Audit(db, "developmentAdminSeeded", "development-seed", subject: account.MaTaiKhoan);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
