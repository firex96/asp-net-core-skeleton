using AspNetCoreSkeletonApplication.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace AspNetCoreSkeletonApplication.UnitTests.Configuration;

public class SecretSourceExtensionsTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Testing")]
    public void AddSecretSource_WithoutSecretProfile_AddsNoSource(string environmentName)
    {
        var configuration = new ConfigurationManager();
        var sourceCount = SourceCount(configuration);

        SecretSourceExtensions.AddSecretSource(configuration, environmentName, typeof(Program).Assembly);

        Assert.Equal(sourceCount, SourceCount(configuration));
    }

    [Fact]
    public void AddSecretSource_Local_AddsUserSecrets()
    {
        var configuration = new ConfigurationManager();
        var sourceCount = SourceCount(configuration);

        SecretSourceExtensions.AddSecretSource(configuration, SecretSourceExtensions.Local, typeof(Program).Assembly);

        var added = Assert.Single(((IConfigurationBuilder)configuration).Sources.Skip(sourceCount));
        Assert.Equal("secrets.json", Assert.IsType<JsonConfigurationSource>(added).Path);
    }

    [Theory]
    [InlineData(SecretSourceExtensions.Aws, "Aws:SecretsManager:SecretId")]
    [InlineData(SecretSourceExtensions.Azure, "Azure:KeyVault:Endpoint")]
    [InlineData(SecretSourceExtensions.Gcp, "Gcp:SecretManager:ProjectId")]
    public void AddSecretSource_CloudProfileWithoutLocation_FailsWithMissingKey(string environmentName, string missingKey)
    {
        var configuration = new ConfigurationManager();

        var exception = Assert.Throws<InvalidOperationException>(
            () => SecretSourceExtensions.AddSecretSource(configuration, environmentName, typeof(Program).Assembly));

        Assert.Contains(missingKey, exception.Message);
    }

    [Fact]
    public void GetRequiredConnectionString_Missing_FailsWithKeyNamesForEachSource()
    {
        var configuration = new ConfigurationManager();

        var exception = Assert.Throws<InvalidOperationException>(
            () => configuration.GetRequiredConnectionString("Default"));

        Assert.Contains("ConnectionStrings:Default", exception.Message);
        Assert.Contains("ConnectionStrings__Default", exception.Message);
        Assert.Contains("ConnectionStrings--Default", exception.Message);
    }

    [Fact]
    public void GetRequiredConnectionString_Present_ReturnsValue()
    {
        var configuration = new ConfigurationManager();
        configuration["ConnectionStrings:Default"] = "Server=example";

        Assert.Equal("Server=example", configuration.GetRequiredConnectionString("Default"));
    }

    private static int SourceCount(IConfigurationBuilder configuration) => configuration.Sources.Count;
}
