using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Mail;

namespace TiemChung.Contracts;

public static class CounterPaymentRules
{
    public const decimal MaxMoney = 9999999999999999m;
    public static bool IsCardBrand(string? brand) => brand is "VISA" or "MASTER" or "JCB" or "ATM" or "Khác";
    public static bool Text([NotNullWhen(true)] string? value, int limit) =>
        value is { Length: > 0 } && value.Length <= limit && !string.IsNullOrWhiteSpace(value) && !value.Any(char.IsControl);
    public static bool IsCash(decimal? cash, decimal total) =>
        cash is { } value && value >= total && value <= MaxMoney && decimal.Truncate(value) == value;
    public static bool IsSourceCharacter(char value) => char.IsAsciiLetterOrDigit(value) || "+-_.@".Contains(value);

    public static PaymentPayer? NormalizePayer(PaymentPayer? payer)
    {
        if (payer is null) return null;
        var name = payer.FullName?.Trim();
        var phone = payer.PhoneNumber?.Trim();
        var email = Optional(payer.Email);
        var province = Optional(payer.ProvinceCity);
        var ward = Optional(payer.WardCommune);
        var address = Optional(payer.AddressLine);
        if (!Text(name, 150) || phone is not { Length: 10 or 11 } || !phone.All(char.IsAsciiDigit) ||
            email is not null && (!Text(email, 254) || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email) ||
            province is not null && !Text(province, 100) || ward is not null && !Text(ward, 100) ||
            address is not null && !Text(address, 250)) return null;
        return new(name, phone, email, province, ward, address);
    }

    public static string? Validate(RecordPaymentRequest request, decimal total, DateTimeOffset now)
    {
        if (NormalizePayer(request.Payer) is null)
            return "Kiểm tra họ tên, số điện thoại 10–11 chữ số, email và địa chỉ người thanh toán.";
        switch (request.Method) {
            case "TienMat" when request.Card is null && request.Transfer is null:
                return IsCash(request.CashTendered, total) ? null : "Tiền khách đưa phải là số nguyên VND và đủ tổng phải thu.";
            case "TheTaiQuay" when request.CashTendered is null && request.Transfer is null && request.Card is { } card:
                if (!IsCardBrand(card.CardBrand) || !Text(card.CardholderName?.Trim(), 150) ||
                    !Text(card.AuthorizationReference?.Trim(), 100))
                    return "Chọn loại thẻ và nhập tên chủ thẻ, mã xác thực.";
                if (!IsCardNumber(card.CardNumber)) return "Số thẻ phải có 12–19 chữ số và đúng định dạng.";
                if (!ValidExpiry(card.ExpiryMonth, card.ExpiryYear, now))
                    return "Nhập tháng và năm hết hạn hợp lệ theo giờ Việt Nam.";
                return null;
            case "ChuyenKhoanQR" when request.CashTendered is null && request.Card is null && request.Transfer is { } transfer:
                return ValidTransfer(transfer) ? null :
                    "Nhập ngân hàng hoặc ví, tên chủ tài khoản, định danh nguồn và mã tham chiếu hợp lệ.";
            default:
                return "Chọn và điền đúng một phương thức thanh toán tại trung tâm.";
        }
    }

    internal static bool ValidTransfer(CounterTransfer transfer)
    {
        var source = transfer.SourceAccountOrWalletId?.Trim();
        return Text(transfer.SourceBankOrWallet?.Trim(), 100) && Text(transfer.AccountHolderName?.Trim(), 150) &&
            Text(source, 100) && source.All(IsSourceCharacter) && source.Any(char.IsAsciiLetterOrDigit) &&
            Text(transfer.TransferReference?.Trim(), 100);
    }

    internal static bool ValidExpiry(int? month, int? year, DateTimeOffset now)
    {
        var local = now.ToOffset(TimeSpan.FromHours(7));
        return month is >= 1 and <= 12 && year is >= 1 and <= 9999 &&
            (year > local.Year || year == local.Year && month >= local.Month);
    }

    internal static bool IsCardNumber([NotNullWhen(true)] string? value)
    {
        if (value is not { Length: >= 12 and <= 40 } || value.Any(c => c != ' ' && !char.IsAsciiDigit(c))) return false;
        var digits = CardDigits(value);
        if (digits.Length is < 12 or > 19) return false;
        var sum = 0;
        for (var i = digits.Length - 1; i >= 0; i--) {
            var digit = digits[i] - '0';
            if ((digits.Length - i) % 2 == 0) digit *= 2;
            sum += digit > 9 ? digit - 9 : digit;
        }
        return sum % 10 == 0;
    }

    public static string CardDigits(string value) => value.Replace(" ", "");
    public static string Mask(string value) => "****" + value[^Math.Min(4, value.Length - 1)..];
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
