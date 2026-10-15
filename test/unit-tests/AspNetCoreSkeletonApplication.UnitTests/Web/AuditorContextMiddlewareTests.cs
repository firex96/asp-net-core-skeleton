using System.Security.Claims;
using AspNetCoreSkeletonApplication.Common.Contexts;
using AspNetCoreSkeletonApplication.Common.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.UnitTests.Web;

public class AuditorContextMiddlewareTests
{
    [Fact]
    public async Task Header_IsIgnoredUnlessTrusted()
    {
        var auditor = await ResolveAsync(trustUserHeader: false, userIdHeader: "42");

        Assert.Null(auditor);
    }

    [Fact]
    public async Task TrustedHeader_BecomesAuditor()
    {
        var auditor = await ResolveAsync(trustUserHeader: true, userIdHeader: "42");

        Assert.Equal(42, auditor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("99999999999999999999")]
    public async Task TrustedHeader_NotALong_IsDropped(string userIdHeader)
    {
        var auditor = await ResolveAsync(trustUserHeader: true, userIdHeader: userIdHeader);

        Assert.Null(auditor);
    }

    [Fact]
    public async Task AuthenticatedUser_WinsOverHeader()
    {
        var auditor = await ResolveAsync(trustUserHeader: true, userIdHeader: "42", authenticatedUserId: "7");

        Assert.Equal(7, auditor);
    }

    [Fact]
    public async Task Context_IsRestoredAfterRequest()
    {
        await ResolveAsync(trustUserHeader: true, userIdHeader: "42");

        Assert.Null(AuditorContext.Current);
    }

    private static async Task<long?> ResolveAsync(bool trustUserHeader, string userIdHeader, string? authenticatedUserId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Id"] = userIdHeader;
        if (authenticatedUserId is not null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, authenticatedUserId)], authenticationType: "Test"));
        }

        long? seen = null;
        var middleware = new AuditorContextMiddleware(
            _ =>
            {
                seen = AuditorContext.Current;
                return Task.CompletedTask;
            },
            Options.Create(new AuditOptions { TrustUserHeader = trustUserHeader }));

        await middleware.InvokeAsync(context);
        return seen;
    }
}
