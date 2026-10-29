using TiemChung.Domain.Rules;
using Xunit;

namespace TiemChung.Tests;

public class RegistrationTests
{
    [Fact]
    public void MixedSinglesAndPackagesArePaidInOneFullPayment()
    {
        var total = RegistrationRules.CalculateTotal([new(2, 450000m, false), new(1, 1200000m, true)]);
        Assert.Equal(2100000m, total);
        Assert.Equal(total, RegistrationRules.RemainingAmount(total, []));
        Assert.Equal(0m, RegistrationRules.RemainingAmount(total, [total]));
    }
    [Fact]
    public void EmptyRegistrationIsRejected() => Assert.Throws<ArgumentException>(() => RegistrationRules.CalculateTotal([]));
    [Theory]
    [InlineData(0, 100)]
    [InlineData(-1, 100)]
    [InlineData(1, -1)]
    public void InvalidLineIsRejected(int qty, decimal price) =>
        Assert.Throws<ArgumentException>(() => RegistrationRules.CalculateTotal([new(qty, price, false)]));
    [Fact]
    public void OverpaymentIsRejected() => Assert.Throws<ArgumentException>(() => RegistrationRules.RemainingAmount(100m, [60m, 50m]));
    [Fact]
    public void ZeroOrNegativePaymentIsRejected() => Assert.Throws<ArgumentException>(() => RegistrationRules.RemainingAmount(100m, [0m]));

    [Fact]
    public void PartialAndRepeatedPaymentsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => RegistrationRules.RemainingAmount(100m, [50m]));
        Assert.Throws<ArgumentException>(() => RegistrationRules.RemainingAmount(100m, [50m, 50m]));
        Assert.Throws<ArgumentException>(() => RegistrationRules.RemainingAmount(100m, [100m, 100m]));
        Assert.Throws<ArgumentException>(() => RegistrationRules.RemainingAmount(100m, [-1m]));
        Assert.Equal(0m, RegistrationRules.RemainingAmount(0m, []));
    }
}
