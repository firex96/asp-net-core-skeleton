using System.Security.Claims;
using AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AspNetCoreSkeletonApplication.UnitTests.Web;

/// <summary>권한 정책은 이름에서 즉석으로 만들고, 판정은 쿠키 클레임이 아니라 <see cref="IPermissionEvaluator"/> 에 묻는다.</summary>
public class PermissionAuthorizationTests
{
    [Fact]
    public async Task PolicyProvider_BuildsPermissionPolicyFromItsName()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync(new HasPermissionAttribute("products.manage").Policy!);

        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements, requirement => requirement is PermissionRequirement { Permission: "products.manage" });
        Assert.Contains(policy.Requirements, requirement => requirement is Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task PolicyProvider_LeavesOtherPoliciesToTheDefaultProvider()
    {
        var options = new AuthorizationOptions();
        options.AddPolicy("Other", policy => policy.RequireClaim("x"));
        var provider = new PermissionPolicyProvider(Options.Create(options));

        Assert.NotNull(await provider.GetPolicyAsync("Other"));
        Assert.Null(await provider.GetPolicyAsync("Unknown"));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Handler_AsksTheEvaluatorForTheSignedInUser(bool granted, bool expected)
    {
        var evaluator = Substitute.For<IPermissionEvaluator>();
        evaluator.HasPermissionAsync(7, "products.manage", Arg.Any<CancellationToken>()).Returns(granted);
        var context = await AuthorizeAsync(evaluator, SignedIn("7"));

        Assert.Equal(expected, context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_WithoutUserId_DoesNotAskAndDoesNotSucceed()
    {
        var evaluator = Substitute.For<IPermissionEvaluator>();
        var context = await AuthorizeAsync(evaluator, new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.False(context.HasSucceeded);
        await evaluator.DidNotReceiveWithAnyArgs().HasPermissionAsync(default, default!, TestContext.Current.CancellationToken);
    }

    private static async Task<AuthorizationHandlerContext> AuthorizeAsync(IPermissionEvaluator evaluator, ClaimsPrincipal user)
    {
        var requirement = new PermissionRequirement("products.manage");
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);
        await new PermissionAuthorizationHandler(evaluator, new HttpContextAccessor()).HandleAsync(context);
        return context;
    }

    private static ClaimsPrincipal SignedIn(string id) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id)], "Test"));
}
