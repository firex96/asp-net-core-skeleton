using AspNetCoreSkeletonApplication.Common.Authorization;
using OpenIddict.Abstractions;

namespace AspNetCoreSkeletonApplication.Common.Web.OAuth;

/// <summary>
/// OAuth 서버가 아는 scope — OIDC 표준 넷과 권한 코드 카탈로그 전부. 권한 코드 scope 는 "이 토큰으로 그 권한을 써도 된다" 는 뜻이다:
/// 사용자 토큰의 실효 권한은 사용자 권한 ∩ 토큰 scope, 클라이언트 토큰은 scope 그대로다(README 「OAuth 서버」).
/// </summary>
public static class OAuthScopes
{
    public static readonly IReadOnlyDictionary<string, string> Standard = new Dictionary<string, string>
    {
        [OpenIddictConstants.Scopes.OpenId] = "로그인한 사용자 식별",
        [OpenIddictConstants.Scopes.Profile] = "이름",
        [OpenIddictConstants.Scopes.Email] = "이메일 주소",
        [OpenIddictConstants.Scopes.OfflineAccess] = "로그아웃해도 계속 접근(리프레시 토큰)",
    };

    public static IEnumerable<string> All => Standard.Keys.Concat(Permissions.All.Select(permission => permission.Code));

    public static string DescriptionOf(string scope) =>
        Standard.TryGetValue(scope, out var description)
            ? description
            : Permissions.All.FirstOrDefault(permission => permission.Code == scope)?.Description ?? scope;
}
