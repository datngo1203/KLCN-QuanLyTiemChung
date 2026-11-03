namespace TiemChung.Contracts;

public sealed record ReceiveInventoryRequest(string LotNumber, int VaccineId, DateOnly ManufacturedOn,
    DateOnly ExpiresOn, int Quantity, string SupplierName, string DocumentNumber, string StorageLocation,
    bool InspectionConfirmed);
public sealed record InventoryReceiptDto(ReceiveInventoryRequest Received, string VaccineName,
    int MovementId, int EmployeeId, string EmployeeName, DateTimeOffset RecordedAt);
public sealed record InventoryVaccineDto(int Id, string Name);
public sealed record InventoryVaccineListResponse(InventoryVaccineDto[] Items, bool HasMore);
