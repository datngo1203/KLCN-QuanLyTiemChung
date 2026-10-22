namespace TiemChung.Contracts;

// --- DTO cho Khách hàng / Người bảo trợ (Customers) ---
public sealed record CustomerDto(
    int Id,
    string FullName,
    string IdentityNumber,
    string PhoneNumber,
    string Email);

public sealed record CreateCustomerRequest(
    string FullName,
    string IdentityNumber,
    string PhoneNumber,
    string Email);

public sealed record CustomerListResponse(
    IReadOnlyList<CustomerDto> Items);

// --- DTO cho Người được tiêm (Recipients) ---
public sealed record RecipientDto(
    int Id,
    int CustomerId,
    string FullName,
    string? IdentityNumber,
    DateOnly DateOfBirth,
    string Gender,
    string RelationshipToCustomer);

public sealed record CreateRecipientRequest(
    int CustomerId,
    string FullName,
    string? IdentityNumber,
    DateOnly DateOfBirth,
    string Gender,
    string RelationshipToCustomer);

public sealed record UpdateRecipientRequest(
    string FullName,
    string? IdentityNumber,
    DateOnly DateOfBirth,
    string Gender,
    string RelationshipToCustomer);

public sealed record RecipientListResponse(
    IReadOnlyList<RecipientDto> Items);