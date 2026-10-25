using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class IdentityModelTests
{
    [Fact]
    public void ModelHasConcurrencyAndFilteredUniquenessWithoutConnectingToSql()
    {
        using var db = new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true").Options);
        var account = db.Model.FindEntityType(typeof(TaiKhoan))!;
        Assert.True(account.FindProperty(nameof(TaiKhoan.MaNhanVien))!.IsNullable);
        Assert.True(account.FindProperty(nameof(TaiKhoan.RowVersion))!.IsConcurrencyToken);
        var refresh = db.Model.FindEntityType(typeof(RefreshCredential))!;
        Assert.Contains(refresh.GetIndexes(), x => x.IsUnique && x.GetFilter()=="[ConsumedAt] IS NULL AND [RevokedAt] IS NULL");
        var recovery = db.Model.FindEntityType(typeof(IdentityRecoveryOperation))!;
        Assert.Contains(recovery.GetIndexes(), x => x.IsUnique && x.Properties.Select(p=>p.Name)
            .SequenceEqual(["RecoveryDatabaseId","M1UpgradeEpoch"]));
        Assert.Equal(2, db.Model.FindEntityType(typeof(AccountProfileLink))!.GetIndexes()
            .Count(x=>x.IsUnique && x.Properties.Count==1 && x.Properties[0].Name=="AccountId"));
    }
}
