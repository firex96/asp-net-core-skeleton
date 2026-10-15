using AspNetCoreSkeletonApplication.Batch.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.Batch.Security;

public static class BatchSecurityExtensions
{
    /// <summary>
    /// 웹훅 API 키 인증과 Job 단위 인가, 대시보드 Basic 인증. 키 목록과 대시보드 계정은 기동할 때 검증한다 —
    /// 이름 없음 · 짧은 키 · 같은 이름 중복, 아이디만 있고 짧은 비밀번호는 기동 실패.
    /// </summary>
    public static IServiceCollection AddBatchSecurity(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null)
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationHandler.SchemeName, null);

        services.AddAuthorizationBuilder()
            .AddPolicy(BatchPolicies.JobTrigger, policy => policy
                .AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName)
                .RequireAuthenticatedUser()
                .AddRequirements(new AllowedJobRequirement()))
            .AddPolicy(BatchPolicies.Operator, policy => policy
                .AddAuthenticationSchemes(BasicAuthenticationHandler.SchemeName)
                .RequireAuthenticatedUser());

        services.AddSingleton<IValidateOptions<BatchOptions>, WebhookOptionsValidator>();
        services.AddOptions<BatchOptions>().ValidateOnStart();

        return services;
    }

    private sealed class WebhookOptionsValidator : IValidateOptions<BatchOptions>
    {
        private const int MinimumKeyLength = 32;
        private const int MinimumPasswordLength = 16;

        public ValidateOptionsResult Validate(string? name, BatchOptions options)
        {
            var keys = options.Webhook.ApiKeys;
            var failures = new List<string>();
            failures.AddRange(keys.Where(key => string.IsNullOrWhiteSpace(key.Name))
                .Select(_ => "Batch:Webhook:ApiKeys 에 Name 이 없는 키가 있다"));
            failures.AddRange(keys.Where(key => key.Key.Length < MinimumKeyLength)
                .Select(key => $"Batch:Webhook:ApiKeys '{key.Name}' 의 Key 가 {MinimumKeyLength}자보다 짧다(또는 없다)"));
            failures.AddRange(keys.GroupBy(key => key.Name).Where(group => group.Count() > 1)
                .Select(group => $"Batch:Webhook:ApiKeys 에 이름 '{group.Key}' 가 여러 번 있다"));
            if (options.Dashboard.Username.Length > 0 && options.Dashboard.Password.Length < MinimumPasswordLength)
            {
                failures.Add($"Batch:Dashboard:Password 가 {MinimumPasswordLength}자보다 짧다(또는 없다)");
            }

            return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
        }
    }
}
