using System.Security.Claims;
using AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
using Microsoft.AspNetCore.Http;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Support;

/// <summary>권한 판정 대역 — 그룹 · 역할 DB 대신 현재 요청의 <c>X-Test-User</c> 헤더에 적힌 권한으로 답한다.</summary>
public sealed class ClaimsPermissionEvaluator(IHttpContextAccessor httpContextAccessor) : IPermissionEvaluator
{
    public Task<IReadOnlySet<string>> ListPermissionsAsync(long userId, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
        IReadOnlySet<string> permissions = UserClaims.IdOf(user) == userId
            ? user.FindAll(TestAuthenticationHandler.PermissionClaimType).Select(claim => claim.Value).ToHashSet()
            : new HashSet<string>();
        return Task.FromResult(permissions);
    }

    public async Task<bool> HasPermissionAsync(long userId, string permission, CancellationToken cancellationToken) =>
        (await ListPermissionsAsync(userId, cancellationToken)).Contains(permission);
}
