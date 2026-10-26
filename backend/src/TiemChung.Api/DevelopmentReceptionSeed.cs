using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class DevelopmentReceptionSeed
{
    public static async Task RunAsync(TiemChungDbContext db, IHostEnvironment environment, IConfiguration config, CancellationToken ct = default)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Reception seed is Development only.");
        var username = config["DevelopmentSeed:Username"] ?? "";
        var password = config["DevelopmentSeed:Password"] ?? "";
        if (username.Length is < 4 or > 100 || username != username.Trim() ||
            username.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('.' or '_' or '-')) || password.Length is < 12 or > 128)
            throw new InvalidOperationException("Set valid DevelopmentSeed username/password in local environment.");

        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        await IdentityTransactions.AdministrationLockAsync(db, ct);
        var normalized = IdentityRegistration.Normalize(username);
        var existing = await db.Set<TaiKhoan>().SingleOrDefaultAsync(a => a.NormalizedUsername == normalized, ct);
        if (existing is not null) {
            if (existing.AccountType != "Employee" || !await db.Set<NhanVien>().AnyAsync(e =>
                e.MaNhanVien == existing.MaNhanVien && e.ChucDanh == "LeTan", ct))
                throw new InvalidOperationException("Seed username already exists with another role; no account was changed.");
            return; // Preserve password, status, grants and employee data on every rerun.
        }
        var employee = new NhanVien { HoTen = "[DEMO] Lễ tân", CCCD = "", SoDienThoai = "", Email = "",
            BoPhan = "Development", ChucDanh = "LeTan", TrangThai = "Active" };
        db.Add(employee);
        await db.SaveChangesAsync(ct);
        var account = new TaiKhoan { MaNhanVien = employee.MaNhanVien, AccountType = "Employee", TenDangNhap = username,
            NormalizedUsername = normalized, TrangThai = "Active", AdministrativeDuty = "None", CreatedAt = DateTimeOffset.UtcNow };
        account.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(account, password);
        db.Add(account);
        await db.SaveChangesAsync(ct);
        IdentityTransactions.Audit(db, "developmentReceptionSeeded", "development-seed", subject: account.MaTaiKhoan);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
