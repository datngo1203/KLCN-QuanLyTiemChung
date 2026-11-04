namespace TiemChung.Contracts;

// --- DTO cho Khách hàng / Người bảo trợ (Customers) ---
public sealed record CustomerDto(
    int Id,
    string FullName,
    string? IdentityNumber,
    string PhoneNumber,
    string Email,
    string? Salutation = null,
    DateOnly? DateOfBirth = null);

public sealed record CreateCustomerRequest(
    string FullName,
    string? IdentityNumber,
    string PhoneNumber,
    string Email,
    string Salutation,
    DateOnly DateOfBirth);

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
    string RelationshipToCustomer,
    string? ProfileCode = null);

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
