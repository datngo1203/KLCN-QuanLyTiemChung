namespace TiemChung.Desktop.Services;

public sealed record PaymentRecord(int Id, int RegistrationId, decimal Amount, string Method, string Type, string Status, DateTime OccurredAt)
{
    public string Code => $"TT-{Id:0000}";
    public string AmountLabel => Amount.ToString("N0") + " đ";
    public string TimeLabel => OccurredAt.ToString("dd/MM/yyyy HH:mm");
}
public sealed record PaymentInvoice(int RegistrationId, string RecipientName, decimal Total, IReadOnlyList<PaymentRecord> History)
{
    public string Code => $"DK-{RegistrationId:0000}";
    public decimal Paid => History.Where(x => x.Status == "Thành công").Sum(x => x.Amount);
    public decimal Remaining => Math.Max(0, Total - Paid);
    public string TotalLabel => Total.ToString("N0") + " đ";
    public string PaidLabel => Paid.ToString("N0") + " đ";
    public string RemainingLabel => Remaining.ToString("N0") + " đ";
    public string State => Remaining == 0 ? "Đã hoàn tất" : Paid > 0 ? "Thanh toán một phần" : "Chờ thanh toán";
}

// TODO: Replace with reviewed payment read/write contract, API authorization and idempotency key.
public interface IPaymentService
{
    bool UseSampleData { get; set; }
    IReadOnlyList<PaymentInvoice> GetInvoices();
    PaymentRecord AddPayment(int registrationId, decimal amount, string method, string type, string status);
}

public sealed class SamplePaymentService : IPaymentService
{
    private readonly List<(int Id, string Recipient, decimal Total)> _invoices = [(1, "Nguyễn Gia Minh", 250000m), (2, "Trần An Bình", 500000m), (3, "Lê Mai Anh", 180000m)];
    private readonly List<PaymentRecord> _payments = [new(1, 2, 100000m, "Tiền mặt", "Đặt cọc", "Thành công", DateTime.Now.AddDays(-1)), new(2, 2, 50000m, "Chuyển khoản", "Trả góp / Một phần", "Thất bại", DateTime.Now.AddHours(-3)), new(3, 3, 180000m, "Thẻ ngân hàng", "Toàn bộ", "Thành công", DateTime.Now.AddDays(-2))];
    public bool UseSampleData { get; set; } = true;
    public IReadOnlyList<PaymentInvoice> GetInvoices() => UseSampleData ? _invoices.Select(x => new PaymentInvoice(x.Id, x.Recipient, x.Total, _payments.Where(p => p.RegistrationId == x.Id).ToArray())).ToArray() : [];
    public PaymentRecord AddPayment(int registrationId, decimal amount, string method, string type, string status)
    {
        if (!UseSampleData) throw new InvalidOperationException("Chưa có nguồn dữ liệu.");
        var invoice = GetInvoices().First(x => x.RegistrationId == registrationId);
        if (amount <= 0 || amount > invoice.Remaining) throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền phải lớn hơn 0 và không vượt số còn lại.");
        var row = new PaymentRecord(_payments.Max(x => x.Id) + 1, registrationId, amount, method, type, status, DateTime.Now);
        _payments.Add(row); return row;
    }
}
