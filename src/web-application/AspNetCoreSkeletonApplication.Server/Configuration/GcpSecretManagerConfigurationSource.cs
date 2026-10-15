using Google.Cloud.SecretManager.V1;

namespace AspNetCoreSkeletonApplication.Server.Configuration;

/// <summary>
/// GCP Secret Manager 에서 <see cref="SecretNames"/> 에 적은 비밀의 최신 버전을 읽는다.
/// Secret Manager 는 구성 공급자를 공식 제공하지 않고 이름으로 일괄 노출하지도 않으므로 읽을 비밀을 명시한다.
/// </summary>
public sealed class GcpSecretManagerConfigurationSource : IConfigurationSource
{
    public required string ProjectId { get; init; }

    public required IReadOnlyList<string> SecretNames { get; init; }

    /// <summary>비밀 이름 → 값. 비우면 Application Default Credentials 로 Secret Manager 를 호출한다 (테스트에서 바꿔 끼운다).</summary>
    public Func<string, string>? AccessSecret { get; init; }

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new GcpSecretManagerConfigurationProvider(this);

    /// <summary>비밀 이름의 <c>--</c> 를 구성 키 구분자로 바꾼다 (Azure Key Vault 공급자와 같은 규칙).</summary>
    public static string ToConfigurationKey(string secretName) =>
        secretName.Replace("--", ConfigurationPath.KeyDelimiter);
}

internal sealed class GcpSecretManagerConfigurationProvider(GcpSecretManagerConfigurationSource source) : ConfigurationProvider
{
    public override void Load()
    {
        var accessSecret = source.AccessSecret ?? CreateClientAccessor(source.ProjectId);
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var secretName in source.SecretNames)
        {
            data[GcpSecretManagerConfigurationSource.ToConfigurationKey(secretName)] = accessSecret(secretName);
        }

        Data = data;
    }

    private static Func<string, string> CreateClientAccessor(string projectId)
    {
        var client = SecretManagerServiceClient.Create();
        return secretName => client
            .AccessSecretVersion(SecretVersionName.FromProjectSecretSecretVersion(projectId, secretName, "latest"))
            .Payload.Data.ToStringUtf8();
    }
}
