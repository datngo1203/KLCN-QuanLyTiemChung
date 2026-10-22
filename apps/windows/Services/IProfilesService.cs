namespace TiemChung.Desktop.Services;

public sealed record CustomerRecord(int Id, string Name, string NationalId, string Phone, string Email)
{
    public override string ToString() => Name;
}
public sealed record RecipientRecord(int Id, int CustomerId, string Name, string? NationalId, DateOnly BirthDate, string Gender, string Relation)
{
    public override string ToString() => Name;
}

// TODO: Replace with reviewed Profiles read/write contracts and server-side duplicate checks.
public interface IProfilesService
{
    bool UseSampleData { get; set; }
    IReadOnlyList<CustomerRecord> GetCustomers();
    IReadOnlyList<RecipientRecord> GetRecipients();
    bool NationalIdExists(string nationalId, int? exceptCustomerId = null);
    CustomerRecord SaveCustomer(CustomerRecord customer);
    RecipientRecord SaveRecipient(RecipientRecord recipient);
}

public sealed class SampleProfilesService : IProfilesService
{
    private readonly List<CustomerRecord> _customers = [new(1, "Nguyễn Minh An", "012345678901", "0901234567", "minhan@example.test")];
    private readonly List<RecipientRecord> _recipients = [new(1, 1, "Nguyễn Gia Minh", null, new DateOnly(2018, 5, 12), "Nam", "Con")];
    public bool UseSampleData { get; set; } = true;
    public IReadOnlyList<CustomerRecord> GetCustomers() => UseSampleData ? _customers.ToArray() : [];
    public IReadOnlyList<RecipientRecord> GetRecipients() => UseSampleData ? _recipients.ToArray() : [];
    public bool NationalIdExists(string nationalId, int? exceptCustomerId = null) => _customers.Any(x => x.NationalId == nationalId && x.Id != exceptCustomerId) || _recipients.Any(x => x.NationalId == nationalId);
    public CustomerRecord SaveCustomer(CustomerRecord customer)
    {
        if (!UseSampleData) throw new InvalidOperationException("Chưa có nguồn dữ liệu.");
        var saved = customer with { Id = customer.Id == 0 ? _customers.Max(x => x.Id) + 1 : customer.Id };
        _customers.RemoveAll(x => x.Id == saved.Id);
        _customers.Add(saved);
        return saved;
    }
    public RecipientRecord SaveRecipient(RecipientRecord recipient)
    {
        if (!UseSampleData) throw new InvalidOperationException("Chưa có nguồn dữ liệu.");
        var saved = recipient with { Id = _recipients.Max(x => x.Id) + 1 };
        _recipients.Add(saved);
        return saved;
    }
}
