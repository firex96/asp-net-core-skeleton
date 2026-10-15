using AspNetCoreSkeletonApplication.Common.Web;

namespace AspNetCoreSkeletonApplication.UnitTests.Web;

public class TraceIdSanitizerTests
{
    [Theory]
    [InlineData("4bf92f3577b34da6a3ce929d0e0e4736")]
    [InlineData("3f2c1a9e-7b6d-4c1e-9a8f-2d5e6b7c8a90")]
    [InlineData("gateway.req_1")]
    public void Sanitize_ValidId_KeepsIt(string raw)
    {
        Assert.Equal(raw, TraceIdSanitizer.Sanitize(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("forged\r\nINFO admin login")]
    [InlineData("has space")]
    [InlineData("a,b")]
    public void Sanitize_InvalidId_ReplacesWithValidNewId(string? raw)
    {
        var sanitized = TraceIdSanitizer.Sanitize(raw);

        Assert.NotEqual(raw, sanitized);
        Assert.True(TraceIdSanitizer.IsValid(sanitized));
    }

    [Fact]
    public void Sanitize_TooLongId_ReplacesIt()
    {
        var raw = new string('a', 65);

        Assert.NotEqual(raw, TraceIdSanitizer.Sanitize(raw));
    }
}
