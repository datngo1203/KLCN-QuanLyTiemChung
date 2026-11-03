using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;

namespace TiemChung.Infrastructure.Data;

internal static class InventoryReceiptModel
{
    internal static void Configure(ModelBuilder model)
    {
        var receipt = model.Entity<InventoryReceipt>();
        receipt.ToTable("InventoryReceipt");
        receipt.HasKey(r => r.SoLo);
        receipt.Property(r => r.SoLo).HasColumnType("nvarchar(80)");
        receipt.Property(r => r.SupplierName).HasColumnType("nvarchar(200)").IsRequired();
        receipt.Property(r => r.DocumentNumber).HasColumnType("nvarchar(100)").IsRequired();
        receipt.Property(r => r.StorageLocation).HasColumnType("nvarchar(200)").IsRequired();
        receipt.HasOne<LoVacXin>().WithMany().HasForeignKey(r => r.SoLo).OnDelete(DeleteBehavior.Restrict);
        receipt.HasOne<BienDongKho>().WithMany().HasForeignKey(r => r.MovementId).OnDelete(DeleteBehavior.Restrict);
        receipt.HasIndex(r => r.MovementId).IsUnique();
    }
}
