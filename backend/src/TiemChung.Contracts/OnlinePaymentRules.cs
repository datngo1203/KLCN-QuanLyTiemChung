using System;
using System.Linq;

namespace TiemChung.Contracts;

public static class OnlinePaymentRules
{
    public static PaymentPayer? NormalizePayer(PaymentPayer? payer) =>
        CounterPaymentRules.NormalizePayer(payer) is { ProvinceCity: not null, WardCommune: not null, AddressLine: not null } valid
            ? valid : null;

    public static bool IsInternationalBrand(string? brand) => brand is "VISA" or "MASTER" or "JCB";

    public static bool ValidCard(InternationalCard card, DateTimeOffset now)
    {
        if (!CounterPaymentRules.Text(card.CardholderName?.Trim(), 150) || !CounterPaymentRules.IsCardNumber(card.CardNumber) ||
            !CounterPaymentRules.ValidExpiry(card.ExpiryMonth, card.ExpiryYear, now) || !Digits(card.Cvv, 3, 3)) return false;
        var number = CounterPaymentRules.CardDigits(card.CardNumber);
        var prefix = int.Parse(number.AsSpan(0, 4), System.Globalization.CultureInfo.InvariantCulture);
        return card.CardBrand switch {
            "VISA" => number[0] == '4' && number.Length is 13 or 16 or 19,
            "MASTER" => number.Length == 16 && prefix is (>= 5100 and <= 5599) or (>= 2221 and <= 2720),
            "JCB" => number.Length is >= 16 and <= 19 && prefix is >= 3528 and <= 3589,
            _ => false
        };
    }

    public static bool ValidDomestic(DomesticPayment payment) =>
        CounterPaymentRules.Text(payment.BankName?.Trim(), 100) &&
        CounterPaymentRules.Text(payment.AccountHolderName?.Trim(), 150) &&
        Digits(payment.CardOrAccountNumber?.Trim(), 8, 20) && Digits(payment.Otp, 6, 6);

    public static bool ValidTransfer(CounterTransfer transfer) => CounterPaymentRules.ValidTransfer(transfer);

    private static bool Digits(string? value, int min, int max) =>
        value is not null && value.Length >= min && value.Length <= max && value.All(char.IsAsciiDigit);
}
