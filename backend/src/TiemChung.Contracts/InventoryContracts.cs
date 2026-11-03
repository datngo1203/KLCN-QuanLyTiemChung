namespace TiemChung.Contracts;

public sealed record InventoryBatchDto(string LotNumber, int VaccineId, string VaccineName,
    DateOnly ManufacturedOn, DateOnly ExpiresOn, int InitialQuantity, int Stock, string Status);
public sealed record InventoryBatchListResponse(InventoryBatchDto[] Items, bool HasMore, DateOnly FacilityDate);
public sealed record InventoryMovementDto(int Id, string LotNumber, string Type, int Quantity,
    DateTimeOffset RecordedAt, int EmployeeId, string EmployeeName);
public sealed record InventoryMovementListResponse(InventoryMovementDto[] Items, bool HasMore);
