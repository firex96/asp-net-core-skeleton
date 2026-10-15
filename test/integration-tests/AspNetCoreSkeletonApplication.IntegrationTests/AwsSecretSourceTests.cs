using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using AspNetCoreSkeletonApplication.Server.Configuration;
using Microsoft.Extensions.Configuration;
using Testcontainers.LocalStack;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>Aws 프로파일이 Secrets Manager 의 JSON 비밀에서 구성 키를 읽는지 LocalStack 으로 확인한다.</summary>
public class AwsSecretSourceTests : IAsyncLifetime
{
    private const string SecretId = "skeleton/test";

    private static readonly string[] AwsEnvironmentVariables =
        ["AWS_ENDPOINT_URL", "AWS_ACCESS_KEY_ID", "AWS_SECRET_ACCESS_KEY", "AWS_REGION"];

    private readonly LocalStackContainer _localStack = new LocalStackBuilder("localstack/localstack:4.9").Build();

    public async ValueTask InitializeAsync()
    {
        await _localStack.StartAsync();

        // AWS SDK 기본 체인이 LocalStack 을 보게 한다 — 앱 코드는 운영과 같은 경로를 탄다.
        Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL", _localStack.GetConnectionString());
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test");
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test");
        Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1");

        using var secretsManager = new AmazonSecretsManagerClient();
        await secretsManager.CreateSecretAsync(new CreateSecretRequest
        {
            Name = SecretId,
            SecretString = """{"ConnectionStrings:Default":"Server=from-secrets-manager"}""",
        });
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var name in AwsEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(name, null);
        }

        await _localStack.DisposeAsync();
    }

    [Fact]
    public void AddSecretSource_Aws_ReadsJsonSecretKeys()
    {
        var configuration = new ConfigurationManager();
        configuration["Aws:SecretsManager:SecretId"] = SecretId;

        SecretSourceExtensions.AddSecretSource(configuration, SecretSourceExtensions.Aws);

        Assert.Equal("Server=from-secrets-manager", configuration.GetRequiredConnectionString("Default"));
    }
}
