namespace TiemChung.Desktop.Services;

public sealed record RegistrationCatalogItem(int Id, string Name, bool IsPackage, decimal Price, int Stock)
{
    public override string ToString() => Name;
}
public sealed record RegistrationLine(int ItemId, string Name, bool IsPackage, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
    public string Kind => IsPackage ? "Gói" : "Vắc xin lẻ";
    public string QuantityLabel => Quantity.ToString();
    public string PriceLabel => UnitPrice.ToString("N0") + " đ";
    public string TotalLabel => Total.ToString("N0") + " đ";
}
public sealed record RegistrationRecord(int Id, int CustomerId, int RecipientId, string CustomerName, string RecipientName, string InjectionType, DateTime PlannedAt, IReadOnlyList<RegistrationLine> Lines, string? PromotionCode, decimal Total, string Status)
{
    public string Code => $"DK-{Id:0000}";
    public string TotalLabel => Total.ToString("N0") + " đ";
}

// TODO: Replace with reviewed task-scoped catalog/stock and registration contracts.
public interface IRegistrationService
{
    bool UseSampleData { get; set; }
    IReadOnlyList<CustomerRecord> GetCustomers();
    IReadOnlyList<RecipientRecord> GetRecipients();
    IReadOnlyList<RegistrationCatalogItem> GetCatalog();
    IReadOnlyList<RegistrationRecord> GetRegistrations();
    RegistrationRecord SaveRegistration(RegistrationRecord registration);
    void AddToWaitlist(int customerId, int recipientId, int vaccineId);
}

public sealed class SampleRegistrationService : IRegistrationService
{
    private readonly List<RegistrationRecord> _registrations = [new(1, 1, 1, "Nguyễn Minh An", "Nguyễn Gia Minh", "Tại trung tâm", DateTime.Today.AddDays(1).AddHours(9), [new(1, "Vắc xin mẫu A", false, 1, 250000m)], null, 250000m, "Chờ khám")];
    private readonly List<(int CustomerId, int RecipientId, int VaccineId)> _waitlist = [];
    public bool UseSampleData { get; set; } = true;
    public IReadOnlyList<CustomerRecord> GetCustomers() => UseSampleData ? [new(1, "Nguyễn Minh An", "012345678901", "0901234567", "minhan@example.test")] : [];
    public IReadOnlyList<RecipientRecord> GetRecipients() => UseSampleData ? [new(1, 1, "Nguyễn Gia Minh", null, new DateOnly(2018, 5, 12), "Nam", "Con")] : [];
    public IReadOnlyList<RegistrationCatalogItem> GetCatalog() => UseSampleData ? [new(1, "Vắc xin mẫu A", false, 250000m, 12), new(2, "Vắc xin mẫu B", false, 180000m, 0), new(3, "Gói mẫu C", true, 690000m, 8)] : [];
    public IReadOnlyList<RegistrationRecord> GetRegistrations() => UseSampleData ? _registrations.ToArray() : [];
    public RegistrationRecord SaveRegistration(RegistrationRecord registration)
    {
        if (!UseSampleData) throw new InvalidOperationException("Chưa có nguồn dữ liệu.");
        var saved = registration with { Id = _registrations.Max(x => x.Id) + 1 };
        _registrations.Add(saved); return saved;
    }
    public void AddToWaitlist(int customerId, int recipientId, int vaccineId)
    {
        if (!UseSampleData) throw new InvalidOperationException("Chưa có nguồn dữ liệu.");
        _waitlist.Add((customerId, recipientId, vaccineId));
    }
}
