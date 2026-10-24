using TiemChung.Api;
using Xunit;

namespace TiemChung.Tests;

public sealed class ProfileDateTests
{
    [Fact]
    public void BirthDateBoundaryUsesSaigonMidnightInsteadOfUtcMidnight()
    {
        Assert.Equal(new DateOnly(2026,10,23),ProfileDates.Today(new DateTimeOffset(2026,10,23,16,59,59,TimeSpan.Zero)));
        Assert.Equal(new DateOnly(2026,10,24),ProfileDates.Today(new DateTimeOffset(2026,10,23,17,0,0,TimeSpan.Zero)));
    }
}
