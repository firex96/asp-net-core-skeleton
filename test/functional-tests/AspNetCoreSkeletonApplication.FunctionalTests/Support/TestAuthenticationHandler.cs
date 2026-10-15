using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Support;

/// <summary>
/// 기능 테스트용 인증 — <c>X-Test-User: {id}</c> 또는 <c>{id};{역할}</c> 헤더로 사용자를 정한다. 헤더가 없으면 익명(401 경로).
/// 쿠키 · Identity 저장소 없이 인가 규칙(기본 정책 · 관리자 정책)만 본다. 실제 로그인은 통합 테스트가 본다.
/// </summary>
public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "Test";
    public const string HeaderName = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var value = Request.Headers[HeaderName].ToString();
        if (value.Length == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var parts = value.Split(';');
        var claims = parts.Skip(1).Select(role => new Claim(ClaimTypes.Role, role)).Prepend(new Claim(ClaimTypes.NameIdentifier, parts[0]));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
