using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AspNetCoreSkeletonApplication.Common.Web.Authorization;

/// <summary>권한 정책 — <see cref="RequestPermissions"/> 로 판정한다(쿠키에 굳은 클레임이 아니라 요청 시점의 그룹 · 역할 · 토큰 scope).</summary>
internal sealed class PermissionAuthorizationHandler(RequestPermissions permissions, IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (await permissions.HasAsync(context.User, requirement.Permission, httpContextAccessor.HttpContext?.RequestAborted ?? default))
        {
            context.Succeed(requirement);
        }
    }
}
