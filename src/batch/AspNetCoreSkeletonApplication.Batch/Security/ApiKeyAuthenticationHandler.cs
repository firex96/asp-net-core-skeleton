using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using AspNetCoreSkeletonApplication.Batch.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.Batch.Security;

/// <summary>
/// <c>X-Api-Key</c> 헤더를 구성된 키와 상수 시간으로 비교한다. 맞으면 키 이름(<see cref="ClaimTypes.Name"/>)과 허용된 Job
/// (<see cref="AllowedJobClaim"/>)을 claim 으로 싣는다. 헤더가 없으면 판단하지 않고(401 은 인가 단계가 낸다), 틀리면 실패다.
/// </summary>
internal sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IOptions<BatchOptions> batchOptions) : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, loggerFactory, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    public const string AllowedJobClaim = "allowed_job";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var presented = Request.Headers[HeaderName].ToString();
        if (presented.Length == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presented));
        var matched = batchOptions.Value.Webhook.ApiKeys.FirstOrDefault(apiKey =>
            CryptographicOperations.FixedTimeEquals(presentedHash, SHA256.HashData(Encoding.UTF8.GetBytes(apiKey.Key))));
        if (matched is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("API 키가 맞지 않는다"));
        }

        var claims = matched.AllowedJobs.Select(job => new Claim(AllowedJobClaim, job)).Prepend(new Claim(ClaimTypes.Name, matched.Name));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
