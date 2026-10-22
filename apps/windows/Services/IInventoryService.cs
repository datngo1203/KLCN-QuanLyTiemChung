namespace TiemChung.Desktop.Services;

// TODO: Replace after inventory read/write contracts and concurrency behavior are reviewed.
public interface IInventoryService { bool UseSampleData { get; set; } IReadOnlyList<InventoryBatch> GetBatches(); IReadOnlyList<InventoryMovement> GetMovements(); }
public sealed record InventoryBatch(string Lot, string Vaccine, DateOnly Manufactured, DateOnly Expires, int Initial, int Stock) { public string Status { get { var today=DateOnly.FromDateTime(DateTime.Today); return Expires<today ? "Hết hạn" : Stock==0 ? "Hết hàng" : Expires<=today.AddDays(30) ? "Cận hạn · FEFO" : "Còn hàng"; } } }
public sealed record InventoryMovement(string Type, string Lot, int Quantity, DateTime At);
public sealed class SampleInventoryService : IInventoryService
{
 public bool UseSampleData { get; set; } = true;
 public IReadOnlyList<InventoryBatch> GetBatches() => UseSampleData ? [new("LO-MAU-01","Vắc xin mẫu A",DateOnly.FromDateTime(DateTime.Today.AddMonths(-6)),DateOnly.FromDateTime(DateTime.Today.AddDays(18)),120,32),new("LO-MAU-02","Vắc xin mẫu B",DateOnly.FromDateTime(DateTime.Today.AddMonths(-18)),DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),80,0)] : [];
 public IReadOnlyList<InventoryMovement> GetMovements() => UseSampleData ? [new("Nhập kho","LO-MAU-01",120,DateTime.Today.AddDays(-2)),new("Hủy / hao hụt","LO-MAU-01",2,DateTime.Today.AddDays(-1))] : [];
}
