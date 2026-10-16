using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.Common.Web.Authorization;

/// <summary><c>Permission:{코드}</c> 정책을 즉석에서 만든다 — 로그인 필요 + <see cref="PermissionRequirement"/>. 나머지 정책은 기본 제공자에 맡긴다.</summary>
internal sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public const string PolicyPrefix = "Permission:";

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal))
        {
            return await base.GetPolicyAsync(policyName);
        }

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName[PolicyPrefix.Length..]))
            .Build();
    }
}
