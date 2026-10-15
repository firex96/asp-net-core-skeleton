using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using AspNetCoreSkeletonApplication.Batch.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.Batch.Security;

/// <summary>
/// 대시보드용 Basic 인증. 브라우저가 계정을 묻고 같은 출처의 이후 요청(SignalR 포함)에 함께 보낸다. 계정은 <see cref="DashboardOptions"/> 와
/// 상수 시간으로 비교한다. 계정이 구성되지 않았으면 누구도 통과하지 못한다. HTTPS 에서만 쓴다(Batch 는 HTTPS 로 리다이렉트한다).
/// </summary>
internal sealed class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IOptions<BatchOptions> batchOptions) : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, loggerFactory, encoder)
{
    public const string SchemeName = "Basic";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header)
            || !SchemeName.Equals(header.Scheme, StringComparison.OrdinalIgnoreCase)
            || header.Parameter is null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var account = batchOptions.Value.Dashboard;
        var credentials = TryDecode(header.Parameter);
        if (credentials is null || account.Username.Length == 0
            || !FixedTimeEquals(credentials.Value.Username, account.Username)
            || !FixedTimeEquals(credentials.Value.Password, account.Password))
        {
            return Task.FromResult(AuthenticateResult.Fail("대시보드 계정이 맞지 않는다"));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, account.Username)], SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"Quartz Dashboard\", charset=\"UTF-8\"";
        return base.HandleChallengeAsync(properties);
    }

    private static (string Username, string Password)? TryDecode(string parameter)
    {
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(parameter));
            var separator = decoded.IndexOf(':');
            return separator < 0 ? null : (decoded[..separator], decoded[(separator + 1)..]);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static bool FixedTimeEquals(string presented, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(presented)), SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
