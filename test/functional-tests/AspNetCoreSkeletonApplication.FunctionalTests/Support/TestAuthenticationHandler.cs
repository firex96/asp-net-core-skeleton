using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Support;

/// <summary>
/// 기능 테스트용 인증 — <c>X-Test-User: {id}</c> 또는 <c>{id};{권한},{권한}</c> 헤더로 사용자와 권한을 정한다. 헤더가 없으면 익명(401 경로).
/// 쿠키 · Identity · 그룹 저장소 없이 인가 규칙(기본 정책 · 권한 · 리소스 판정)만 본다 — 권한은 <see cref="ClaimsPermissionEvaluator"/> 가 이 헤더에서 읽는다.
/// 실제 로그인과 그룹 · 역할 판정은 통합 테스트가 본다.
/// </summary>
public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "Test";
    public const string HeaderName = "X-Test-User";
    public const string PermissionClaimType = "test-permission";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var value = Request.Headers[HeaderName].ToString();
        if (value.Length == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var parts = value.Split(';');
        var claims = parts.Skip(1).SelectMany(permissions => permissions.Split(','))
            .Select(permission => new Claim(PermissionClaimType, permission))
            .Prepend(new Claim(ClaimTypes.NameIdentifier, parts[0]));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
