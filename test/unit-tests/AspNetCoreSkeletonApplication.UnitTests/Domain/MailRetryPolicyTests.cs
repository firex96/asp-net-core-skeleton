using AspNetCoreSkeletonApplication.Common.Mail.Domain;

namespace AspNetCoreSkeletonApplication.UnitTests.Domain;

public class MailRetryPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 3, 30, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(1, 30)]
    [InlineData(2, 60)]
    [InlineData(3, 120)]
    [InlineData(7, 1920)]
    public void Delay_DoublesFromThirtySeconds(int attempts, int seconds)
    {
        Assert.Equal(Now.AddSeconds(seconds), MailRetryPolicy.NextAttemptAt(attempts, Now));
    }

    [Fact]
    public void GivesUp_AfterMaxAttempts()
    {
        Assert.Null(MailRetryPolicy.NextAttemptAt(MailRetryPolicy.MaxAttempts, Now));
    }

    [Fact]
    public void Attempts_BelowOne_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MailRetryPolicy.NextAttemptAt(0, Now));
    }
}
