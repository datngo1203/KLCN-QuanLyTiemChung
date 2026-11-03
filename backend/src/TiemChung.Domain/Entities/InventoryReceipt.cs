namespace TiemChung.Domain.Entities;

public sealed class InventoryReceipt
{
    public string SoLo { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public int MovementId { get; set; }
}
