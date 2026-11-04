using System.Globalization;
using System.Runtime.CompilerServices;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class PaymentDraft(RegistrationContact? contact, decimal totalDue) : ViewModelBase
{
    private sealed record Fields(PaymentPayer Payer, string Method = "TienMat", string Cash = "",
        string? Brand = null, string Cardholder = "", string CardNumber = "", string Month = "", string Year = "",
        string Authorization = "", string Provider = "", string Holder = "", string Source = "", string TransferReference = "");

    private readonly Fields _initial = Initial(contact);
    private Fields _fields = Initial(contact);
    private string? _error;
    private static Fields Initial(RegistrationContact? c) => new(new(c?.FullName ?? "", c?.PhoneNumber ?? "",
        c?.Email ?? "", c?.ProvinceCity ?? "", c?.WardCommune ?? "", c?.AddressLine ?? ""));

    public record MethodOption(string Code, string Label);
    public MethodOption[] Methods { get; } = [
        new("TienMat", "Tiền mặt"), new("TheTaiQuay", "Thẻ tại quầy"), new("ChuyenKhoanQR", "Chuyển khoản / QR")
    ];
    public string[] Brands { get; } = ["VISA", "MASTER", "JCB", "ATM", "Khác"];
    public bool IsDirty => _fields != _initial;
    public bool IsCash => Method == "TienMat";
    public bool IsCard => Method == "TheTaiQuay";
    public bool IsTransfer => Method == "ChuyenKhoanQR";
    public string? Error { get => _error; private set => Set(ref _error, value); }
    public string Method { get => _fields.Method; set {
        if (!Methods.Any(m => m.Code == value)) return;
        Update(_fields with { Method = value });
        OnPropertyChanged(nameof(IsCash)); OnPropertyChanged(nameof(IsCard)); OnPropertyChanged(nameof(IsTransfer));
    } }
    public string? FullName { get => _fields.Payer.FullName; set => Update(_fields with { Payer = _fields.Payer with { FullName = value } }); }
    public string? PhoneNumber { get => _fields.Payer.PhoneNumber; set => Update(_fields with { Payer = _fields.Payer with { PhoneNumber = value } }); }
    public string? Email { get => _fields.Payer.Email; set => Update(_fields with { Payer = _fields.Payer with { Email = value } }); }
    public string? ProvinceCity { get => _fields.Payer.ProvinceCity; set => Update(_fields with { Payer = _fields.Payer with { ProvinceCity = value } }); }
    public string? WardCommune { get => _fields.Payer.WardCommune; set => Update(_fields with { Payer = _fields.Payer with { WardCommune = value } }); }
    public string? AddressLine { get => _fields.Payer.AddressLine; set => Update(_fields with { Payer = _fields.Payer with { AddressLine = value } }); }
    public string Cash { get => _fields.Cash; set {
        Update(_fields with { Cash = value });
        OnPropertyChanged(nameof(ChangeLabel));
    } }
    public string? Brand { get => _fields.Brand; set => Update(_fields with { Brand = value }); }
    public string Cardholder { get => _fields.Cardholder; set => Update(_fields with { Cardholder = value }); }
    public string CardNumber { get => _fields.CardNumber; set => Update(_fields with { CardNumber = value }); }
    public string Month { get => _fields.Month; set => Update(_fields with { Month = value }); }
    public string Year { get => _fields.Year; set => Update(_fields with { Year = value }); }
    public string Authorization { get => _fields.Authorization; set => Update(_fields with { Authorization = value }); }
    public string Provider { get => _fields.Provider; set => Update(_fields with { Provider = value }); }
    public string Holder { get => _fields.Holder; set => Update(_fields with { Holder = value }); }
    public string Source { get => _fields.Source; set => Update(_fields with { Source = value }); }
    public string TransferReference { get => _fields.TransferReference; set => Update(_fields with { TransferReference = value }); }

    private decimal? Tendered => decimal.TryParse(Cash, NumberStyles.None, CultureInfo.InvariantCulture, out var cash) ? cash : null;
    public string ChangeLabel => CounterPaymentRules.IsCash(Tendered, totalDue) ? Money(Tendered!.Value - totalDue) : "—";
    internal static string Money(decimal amount) => $"{amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} đ";
    private static int? ParseExpiry(string text) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;

    internal RecordPaymentRequest? Prepare()
    {
        var request = new RecordPaymentRequest(_fields.Payer, Method, IsCash ? Tendered : null,
            IsCard ? new(Brand, Cardholder, CardNumber, ParseExpiry(Month), ParseExpiry(Year), Authorization) : null,
            IsTransfer ? new(Provider, Holder, Source, TransferReference) : null);
        Error = CounterPaymentRules.Validate(request, totalDue, DateTimeOffset.UtcNow);
        return Error is null ? request with { Payer = CounterPaymentRules.NormalizePayer(request.Payer) } : null;
    }

    internal string Review(RecordPaymentRequest request)
    {
        if (request.CashTendered is { } cash) return $"Tiền mặt · Khách đưa {Money(cash)} · Trả lại {Money(cash - totalDue)}";
        if (request.Card is { } card) return $"Thẻ tại quầy · {card.CardBrand} · {card.CardholderName?.Trim()}\n" +
            $"{CounterPaymentRules.Mask(CounterPaymentRules.CardDigits(card.CardNumber!))} · Mã xác thực {card.AuthorizationReference?.Trim()}";
        var transfer = request.Transfer!;
        return $"Chuyển khoản / QR · {transfer.SourceBankOrWallet?.Trim()} · {transfer.AccountHolderName?.Trim()}\n" +
            $"{CounterPaymentRules.Mask(transfer.SourceAccountOrWalletId!.Trim())} · Tham chiếu {transfer.TransferReference?.Trim()}";
    }

    private void Update(Fields fields, [CallerMemberName] string? property = null)
    {
        if (Set(ref _fields, fields, property)) Error = null;
    }
}
