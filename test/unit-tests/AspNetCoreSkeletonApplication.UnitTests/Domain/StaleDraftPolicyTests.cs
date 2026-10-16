using AspNetCoreSkeletonApplication.Common.Product.Domain;

namespace AspNetCoreSkeletonApplication.UnitTests.Domain;

public class StaleDraftPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CutoffOf_IsNowMinusDays()
    {
        Assert.Equal(new DateTimeOffset(2026, 9, 15, 9, 0, 0, TimeSpan.Zero), StaleDraftPolicy.CutoffOf(Now, 30));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CutoffOf_NonPositiveDays_IsRejected(int days)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StaleDraftPolicy.CutoffOf(Now, days));
    }
}
