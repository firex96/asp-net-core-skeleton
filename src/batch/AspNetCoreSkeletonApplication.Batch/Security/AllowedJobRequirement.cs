using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AspNetCoreSkeletonApplication.Batch.Security;

/// <summary>경로의 <c>{jobName}</c> 이 인증된 API 키에 허용된 Job 이어야 한다 — 아니면 403. 없는 Job 이름도 같은 403 이다(존재를 드러내지 않는다).</summary>
internal sealed class AllowedJobRequirement : AuthorizationHandler<AllowedJobRequirement>, IAuthorizationRequirement
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AllowedJobRequirement requirement)
    {
        if (context.Resource is HttpContext httpContext
            && httpContext.GetRouteValue("jobName") is string jobName
            && context.User.HasClaim(ApiKeyAuthenticationHandler.AllowedJobClaim, jobName))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
