using AspNetCoreSkeletonApplication.Common.Mail;

namespace AspNetCoreSkeletonApplication.UnitTests.Web;

public class SmtpMailSenderTests
{
    [Theory]
    [InlineData("abcdef@example.com", "ab***@example.com")]
    [InlineData("a@example.com", "a***@example.com")]
    [InlineData("not-an-address", "***")]
    public void Mask_HidesTheLocalPartButTheFirstTwoCharacters(string address, string expected)
    {
        Assert.Equal(expected, SmtpMailSender.Mask(address));
    }
}
