namespace TiemChung.Contracts;

public sealed record ReceiveInventoryRequest(string LotNumber, int VaccineId, DateOnly ManufacturedOn,
    DateOnly ExpiresOn, int Quantity, string SupplierName, string DocumentNumber, string StorageLocation,
    bool InspectionConfirmed);
public sealed record InventoryReceiptDto(ReceiveInventoryRequest Received, string VaccineName,
    int MovementId, int EmployeeId, string EmployeeName, DateTimeOffset RecordedAt);
public sealed record InventoryVaccineDto(int Id, string Name);
public sealed record InventoryVaccineListResponse(InventoryVaccineDto[] Items, bool HasMore);

// X2.2.1: read-only list of receipts written by X2.1. Quantity is the original receipt movement,
// not mutable current stock. RecordedAt is UTC; client displays it in the local timezone.
public sealed record InventoryReceiptHistoryItemDto(string LotNumber, string VaccineName, int Quantity,
    string SupplierName, string DocumentNumber, string StorageLocation, int MovementId,
    int EmployeeId, string EmployeeName, DateTimeOffset RecordedAt);
public sealed record InventoryReceiptHistoryListResponse(InventoryReceiptHistoryItemDto[] Items, bool HasMore);
