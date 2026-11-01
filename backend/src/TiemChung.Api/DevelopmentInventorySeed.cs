using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class DevelopmentInventorySeed
{
    public static async Task<int> RunAsync(TiemChungDbContext db, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Inventory seed is Development only.");

        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        // Serialize seed processes before reading either catalog or lots, including absent keys.
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @r int;
            EXEC @r=sys.sp_getapplock @Resource=N'KLCN.S5.InventorySeed',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
            IF @r<0 THROW 51001, 'Inventory seed lock unavailable; retry the command.', 1;
            """);

        var vaccineIds = new Dictionary<string, int>();
        foreach (var suffix in new[] { "A", "B" }) {
            var name = "[DEMO] Vắc xin " + suffix;
            var matches = await db.Vaccines.AsNoTracking().Where(x => x.TenVacXin == name).ToArrayAsync();
            if (matches.Length != 1 || matches[0].TenVacXin != name)
                throw new InvalidOperationException($"Inventory seed requires exactly one synthetic catalog vaccine '{name}'. Run --seed-catalog separately on the intended Development database or review the catalog conflict.");
            vaccineIds[suffix] = matches[0].MaVacXin;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        LoVacXin[] lots = [
            new() { SoLo="[DEMO] S5-VALID-A", MaVacXin=vaccineIds["A"], NgaySanXuat=today.AddMonths(-6),
                HanSuDung=today.AddYears(1), SoLuongBanDau=100, SoLuongTon=100, TrangThai="Active" },
            new() { SoLo="[DEMO] S5-EXPIRED-A", MaVacXin=vaccineIds["A"], NgaySanXuat=today.AddYears(-2),
                HanSuDung=today.AddDays(-1), SoLuongBanDau=20, SoLuongTon=20, TrangThai="Active" },
            new() { SoLo="[DEMO] S5-EMPTY-B", MaVacXin=vaccineIds["B"], NgaySanXuat=today.AddMonths(-6),
                HanSuDung=today.AddYears(1), SoLuongBanDau=10, SoLuongTon=0, TrangThai="Active" }
        ];
        var added = 0;
        foreach (var lot in lots) {
            var existing = await db.Set<LoVacXin>().AsNoTracking().SingleOrDefaultAsync(x => x.SoLo == lot.SoLo);
            if (existing is not null) {
                if (existing.SoLo != lot.SoLo || existing.MaVacXin != lot.MaVacXin)
                    throw new InvalidOperationException($"Inventory seed lot '{lot.SoLo}' conflicts with existing data. No existing lot was changed.");
                // Stock, initial quantity, dates and status may have changed since the first seed.
                continue;
            }
            db.Add(lot);
            added++;
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return added;
    }
}
