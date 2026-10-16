using AspNetCoreSkeletonApplication.Common.Mail.Clients;

namespace AspNetCoreSkeletonApplication.UnitTests.Clients;

public class SmtpMailClientTests
{
    [Theory]
    [InlineData("abcdef@example.com", "ab***@example.com")]
    [InlineData("a@example.com", "a***@example.com")]
    [InlineData("not-an-address", "***")]
    public void Mask_HidesTheLocalPartButTheFirstTwoCharacters(string address, string expected)
    {
        Assert.Equal(expected, SmtpMailClient.Mask(address));
    }
}
