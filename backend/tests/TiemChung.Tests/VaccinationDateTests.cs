using TiemChung.Api;
using Xunit;

namespace TiemChung.Tests;

public sealed class VaccinationDateTests
{
    [Theory]
    [InlineData("2026-11-01T16:59:59+00:00",2026,11,1)]
    [InlineData("2026-11-01T17:00:00+00:00",2026,11,2)]
    [InlineData("2026-12-31T17:00:00+00:00",2027,1,1)]
    [InlineData("2026-11-02T00:00:00+07:00",2026,11,2)]
    public void FacilityDateUsesVietnamDayAcrossUtcMidnight(string timestamp,int year,int month,int day) =>
        Assert.Equal(new DateOnly(year,month,day),VaccinationEndpoints.FacilityDate(DateTimeOffset.Parse(timestamp)));
}
