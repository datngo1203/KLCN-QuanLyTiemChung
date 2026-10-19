using TiemChung.Domain.Rules;
using Xunit;

namespace TiemChung.Tests;

public class RegistrationTests
{
    [Fact]
    public void MixedSinglesAndPackagesCanBePaidInSeveralInstallments()
    {
        var total = RegistrationRules.CalculateTotal([new(2, 450000m, false), new(1, 1200000m, true)]);
        Assert.Equal(2100000m, total);
        Assert.Equal(600000m, RegistrationRules.RemainingAmount(total, [500000m, 1000000m]));
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
}
