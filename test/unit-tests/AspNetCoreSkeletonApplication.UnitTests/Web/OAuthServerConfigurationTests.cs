using AspNetCoreSkeletonApplication.Common.Web.OAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AspNetCoreSkeletonApplication.UnitTests.Web;

/// <summary>구성의 클라이언트가 OpenIddict 등록으로 어떻게 바뀌는지, 빠진 구성은 기동을 멈추는지.</summary>
public class OAuthServerConfigurationTests
{
    [Fact]
    public void PublicCodeClient_RequiresPkce_AndGetsOnlyItsScopes()
    {
        var descriptor = OAuthClientSynchronizer.DescriptorOf(new OAuthClientOptions
        {
            ClientId = "app", DisplayName = "App", ConsentType = OAuthConsentType.Explicit,
            GrantTypes = [GrantTypes.AuthorizationCode], RedirectUris = ["https://client.test/cb"], Scopes = [Scopes.OpenId, "products.manage"],
        });

        Assert.Equal(ClientTypes.Public, descriptor.ClientType);
        Assert.Equal(ConsentTypes.Explicit, descriptor.ConsentType);
        Assert.Contains(Requirements.Features.ProofKeyForCodeExchange, descriptor.Requirements);
        Assert.Contains(Permissions.Prefixes.Scope + "products.manage", descriptor.Permissions);
        Assert.DoesNotContain(Permissions.Prefixes.Scope + Scopes.OpenId, descriptor.Permissions);
        Assert.DoesNotContain(Permissions.GrantTypes.ClientCredentials, descriptor.Permissions);
    }

    [Fact]
    public void ServiceClient_IsConfidential_WithoutAuthorizationEndpoint()
    {
        var descriptor = OAuthClientSynchronizer.DescriptorOf(new OAuthClientOptions
        {
            ClientId = "svc", ClientSecret = "secret", DisplayName = "Svc", ConsentType = OAuthConsentType.Implicit,
            GrantTypes = [GrantTypes.ClientCredentials], Scopes = ["products.manage"],
        });

        Assert.Equal(ClientTypes.Confidential, descriptor.ClientType);
        Assert.Contains(Permissions.GrantTypes.ClientCredentials, descriptor.Permissions);
        Assert.DoesNotContain(Permissions.Endpoints.Authorization, descriptor.Permissions);
    }

    [Theory]
    [InlineData(null, null)]                                                   // KeySource 없음
    [InlineData("Certificates", null)]                                         // 인증서 없음
    [InlineData("Ephemeral", "client_credentials")]                            // 비밀 없는 클라이언트 자격 증명
    public void IncompleteConfiguration_FailsValidation(string? keySource, string? secretlessGrant)
    {
        var settings = new Dictionary<string, string?> { ["App:PublicOrigin"] = "https://spa.test" };
        if (keySource is not null)
        {
            settings["Authentication:OAuthServer:KeySource"] = keySource;
        }

        if (secretlessGrant is not null)
        {
            settings["Authentication:OAuthServer:Clients:0:ClientId"] = "svc";
            settings["Authentication:OAuthServer:Clients:0:DisplayName"] = "Svc";
            settings["Authentication:OAuthServer:Clients:0:ConsentType"] = "Implicit";
            settings["Authentication:OAuthServer:Clients:0:GrantTypes:0"] = secretlessGrant;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).AddLogging();

        // 서명 키 구성은 등록할 때, 나머지는 옵션을 처음 읽을 때(기동할 때) 멈춘다.
        Assert.Throws<OptionsValidationException>(() =>
        {
            services.AddSkeletonOAuthServer(configuration);
            using var provider = services.BuildServiceProvider();
            _ = provider.GetRequiredService<IOptions<OAuthServerOptions>>().Value;
        });
    }
}
