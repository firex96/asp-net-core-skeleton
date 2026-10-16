using AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AspNetCoreSkeletonApplication.Common.Web.Authorization;

/// <summary>로그인한 사용자의 권한을 <see cref="IPermissionEvaluator"/> 에 묻는다 — 쿠키에 담긴 클레임이 아니라 요청 시점의 그룹 · 역할로 판정한다.</summary>
internal sealed class PermissionAuthorizationHandler(IPermissionEvaluator evaluator, IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (UserClaims.IdOf(context.User) is { } userId
            && await evaluator.HasPermissionAsync(userId, requirement.Permission, httpContextAccessor.HttpContext?.RequestAborted ?? default))
        {
            context.Succeed(requirement);
        }
    }
}
