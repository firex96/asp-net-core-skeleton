using System.Security.Claims;
using AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;
using AspNetCoreSkeletonApplication.Common.Web.OAuth;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;

namespace AspNetCoreSkeletonApplication.Common.Web.Authorization;

/// <summary>
/// 요청 주체의 권한 — 쿠키로 들어왔는지 Bearer 토큰으로 들어왔는지에 따라 규칙 하나로 판정한다. 정책 핸들러와 리소스 핸들러가 이것을 쓴다.
/// <list type="bullet">
/// <item>쿠키(같은 출처 SPA) — 사용자의 실효 권한(<see cref="IPermissionEvaluator"/>).</item>
/// <item>사용자 토큰 — 사용자의 실효 권한 ∩ 토큰 scope. 사용자가 허락한 범위 밖은 쓸 수 없다.</item>
/// <item>클라이언트 토큰(클라이언트 자격 증명, 주체 <c>client:{id}</c>) — 토큰 scope 그대로. 클라이언트가 받을 수 있는 scope 는 등록이 정한다.</item>
/// </list>
/// </summary>
public sealed class RequestPermissions(IPermissionEvaluator evaluator, IHttpContextAccessor httpContextAccessor)
{
    public async Task<bool> HasAsync(ClaimsPrincipal user, string permission, CancellationToken cancellationToken)
    {
        if (!IsBearer())
        {
            return UserClaims.IdOf(user) is { } userId && await evaluator.HasPermissionAsync(userId, permission, cancellationToken);
        }

        if (!user.HasScope(permission))
        {
            return false;
        }

        return IsClient(user) || (UserClaims.IdOf(user) is { } tokenUserId && await evaluator.HasPermissionAsync(tokenUserId, permission, cancellationToken));
    }

    /// <summary>사용자의 실효 권한. 토큰이면 scope 와의 교집합이다. 클라이언트 토큰에는 사용자가 없으므로 쓰지 않는다.</summary>
    public async Task<IReadOnlySet<string>> ListOfUserAsync(ClaimsPrincipal user, long userId, CancellationToken cancellationToken)
    {
        var permissions = await evaluator.ListPermissionsAsync(userId, cancellationToken);
        return IsBearer() ? permissions.Where(user.HasScope).ToHashSet(StringComparer.Ordinal) : permissions;
    }

    public static bool IsClient(ClaimsPrincipal user) =>
        user.GetClaim(OpenIddictConstants.Claims.Subject)?.StartsWith(OAuthServerEndpoints.ClientSubjectPrefix, StringComparison.Ordinal) ?? false;

    private bool IsBearer() => BearerAuthentication.IsBearer(httpContextAccessor.HttpContext?.Request);
}
