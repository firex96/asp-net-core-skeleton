using Azure.Core;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace AspNetCoreSkeletonApplication.Server.Configuration;

/// <summary>
/// 실행 프로파일별 비밀값 출처를 구성에 붙인다. 프로파일은 비밀값을 어디서 읽을지만 고른다.
/// <list type="bullet">
/// <item><c>Local</c> — appsettings.Local.json(docker compose 값) + user-secrets</item>
/// <item><c>Aws</c> — AWS Secrets Manager 의 JSON 비밀 하나</item>
/// <item><c>Azure</c> — Azure Key Vault 의 모든 비밀</item>
/// <item><c>Gcp</c> — GCP Secret Manager 에서 이름을 적은 비밀만</item>
/// <item>그 밖(미지정 = Production) — 환경변수만. 기본 구성에 이미 들어 있다</item>
/// </list>
/// 비밀 이름의 <c>--</c> 는 구성 키 구분자 <c>:</c> 로 읽힌다 (예: <c>ConnectionStrings--Default</c> → <c>ConnectionStrings:Default</c>).
/// </summary>
public static class SecretSourceExtensions
{
    public const string Local = "Local";
    public const string Aws = "Aws";
    public const string Azure = "Azure";
    public const string Gcp = "Gcp";

    public static WebApplicationBuilder AddSecretSource(this WebApplicationBuilder builder)
    {
        AddSecretSource(builder.Configuration, builder.Environment.EnvironmentName);
        return builder;
    }

    public static IConfigurationManager AddSecretSource(IConfigurationManager configuration, string environmentName)
    {
        if (Is(environmentName, Local))
        {
            // user-secrets 는 Development 환경에서만 자동으로 읽히므로 Local 에서 명시적으로 추가한다.
            configuration.AddUserSecrets<Program>(optional: true);
        }
        else if (Is(environmentName, Aws))
        {
            // Parameter Store 의 예약 경로로 Secrets Manager 비밀을 읽는다. JSON 비밀의 키가 구성 키가 된다.
            var secretId = configuration.GetRequiredValue("Aws:SecretsManager:SecretId");
            configuration.AddSystemsManager($"/aws/reference/secretsmanager/{secretId}");
        }
        else if (Is(environmentName, Azure))
        {
            var endpoint = configuration.GetRequiredValue("Azure:KeyVault:Endpoint");
            var secretClientOptions = new SecretClientOptions
            {
                Retry =
                {
                    Delay = TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential,
                },
            };
            var client = new SecretClient(new Uri(endpoint), new DefaultAzureCredential(), secretClientOptions);
            configuration.AddAzureKeyVault(client, new AzureKeyVaultConfigurationOptions());
        }
        else if (Is(environmentName, Gcp))
        {
            configuration.Add(new GcpSecretManagerConfigurationSource
            {
                ProjectId = configuration.GetRequiredValue("Gcp:SecretManager:ProjectId"),
                SecretNames = configuration.GetSection("Gcp:SecretManager:Secrets").Get<string[]>() ?? [],
            });
        }

        return configuration;
    }

    /// <summary>
    /// 연결 문자열을 읽는다. 없으면 빠진 키와 각 출처에서의 이름을 담아 기동을 멈춘다 — 조용한 미설정을 두지 않는다.
    /// </summary>
    public static string GetRequiredConnectionString(this IConfiguration configuration, string name)
    {
        var value = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"구성 키 'ConnectionStrings:{name}' 가 없다. 환경변수 'ConnectionStrings__{name}' 또는 " +
                $"비밀 저장소의 'ConnectionStrings--{name}' 로 넣는다. README 「설정과 비밀값」 참고.");
        }

        return value;
    }

    private static string GetRequiredValue(this IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"구성 키 '{key}' 가 없다. appsettings 또는 환경변수 '{key.Replace(":", "__")}' 로 넣는다.");
        }

        return value;
    }

    private static bool Is(string environmentName, string profile) =>
        string.Equals(environmentName, profile, StringComparison.OrdinalIgnoreCase);
}
