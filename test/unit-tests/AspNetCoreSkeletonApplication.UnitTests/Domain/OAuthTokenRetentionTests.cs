using AspNetCoreSkeletonApplication.Common.OAuth.Domain;

namespace AspNetCoreSkeletonApplication.UnitTests.Domain;

public class OAuthTokenRetentionTests
{
    [Fact]
    public void Threshold_IsNowMinusRetention()
    {
        var now = new DateTimeOffset(2026, 10, 16, 3, 30, 0, TimeSpan.Zero);

        Assert.Equal(now.AddDays(-14), OAuthTokenRetention.ThresholdOf(now, 14));
    }

    [Fact]
    public void Retention_BelowOneDay_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OAuthTokenRetention.ThresholdOf(DateTimeOffset.UnixEpoch, 0));
    }
}
