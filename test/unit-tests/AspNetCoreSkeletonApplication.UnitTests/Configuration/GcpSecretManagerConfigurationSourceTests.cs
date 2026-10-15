using AspNetCoreSkeletonApplication.Common.Configuration;
using Microsoft.Extensions.Configuration;

namespace AspNetCoreSkeletonApplication.UnitTests.Configuration;

public class GcpSecretManagerConfigurationSourceTests
{
    [Theory]
    [InlineData("ConnectionStrings--Default", "ConnectionStrings:Default")]
    [InlineData("Jwt--Signing--Key", "Jwt:Signing:Key")]
    [InlineData("api-key", "api-key")]
    public void ToConfigurationKey_ReplacesDoubleHyphenWithKeyDelimiter(string secretName, string expected)
    {
        Assert.Equal(expected, GcpSecretManagerConfigurationSource.ToConfigurationKey(secretName));
    }

    [Fact]
    public void Load_ReadsOnlyListedSecretsAsConfigurationKeys()
    {
        var accessed = new List<string>();
        var configuration = new ConfigurationBuilder()
            .Add(new GcpSecretManagerConfigurationSource
            {
                ProjectId = "test-project",
                SecretNames = ["ConnectionStrings--Default"],
                AccessSecret = name =>
                {
                    accessed.Add(name);
                    return $"value-of-{name}";
                },
            })
            .Build();

        Assert.Equal(["ConnectionStrings--Default"], accessed);
        Assert.Equal("value-of-ConnectionStrings--Default", configuration.GetConnectionString("Default"));
    }
}
