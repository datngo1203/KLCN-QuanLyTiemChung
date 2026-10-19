namespace TiemChung.Domain.Rules;

public sealed record RegistrationLine(int Quantity, decimal UnitPrice, bool IsPackage);

public static class RegistrationRules
{
    public static decimal CalculateTotal(IEnumerable<RegistrationLine> items)
    {
        var lines = items.ToArray();
        if (lines.Length == 0) throw new ArgumentException("Đăng ký phải có ít nhất một vắc xin lẻ hoặc gói.");
        if (lines.Any(x => x.Quantity <= 0 || x.UnitPrice < 0))
            throw new ArgumentException("Số lượng phải dương và đơn giá không âm.");
        return lines.Sum(x => x.Quantity * x.UnitPrice);
    }

    // Pass only successful payments; execute the read/check/write in one DB transaction.
    public static decimal RemainingAmount(decimal totalDue, IEnumerable<decimal> successfulPayments)
    {
        if (totalDue < 0) throw new ArgumentOutOfRangeException(nameof(totalDue));
        var payments = successfulPayments.ToArray();
        if (payments.Any(x => x <= 0)) throw new ArgumentException("Khoản thanh toán phải dương.");
        var paid = payments.Sum();
        if (paid > totalDue) throw new ArgumentException("Tổng thanh toán vượt số tiền cần thu.");
        return totalDue - paid;
    }
}
