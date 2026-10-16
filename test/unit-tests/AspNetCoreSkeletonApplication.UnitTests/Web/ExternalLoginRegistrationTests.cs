using AspNetCoreSkeletonApplication.Common.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.UnitTests.Web;

/// <summary>공급자는 구성 섹션이 있을 때만 켜지고, 켜면 ClientId · ClientSecret 은 필수다(빈 값으로 뜨지 않는다).</summary>
public class ExternalLoginRegistrationTests
{
    [Fact]
    public async Task OnlyConfiguredProviders_AreRegistered()
    {
        var schemes = await SchemesAsync(new Dictionary<string, string?>
        {
            ["Authentication:External:Kakao:ClientId"] = "id",
            ["Authentication:External:Kakao:ClientSecret"] = "secret",
        });

        Assert.Contains("Kakao", schemes);
        Assert.DoesNotContain("Google", schemes);
        Assert.DoesNotContain("Naver", schemes);
    }

    [Fact]
    public void ConfiguredProvider_WithoutSecret_StopsRegistration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:External:Google:ClientId"] = "id",
        }).Build();

        var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddSkeletonExternalLogins(configuration));
        Assert.Contains("Authentication:External:Google:ClientSecret", exception.Message);
    }

    private static async Task<string[]> SchemesAsync(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddLogging();
        services.AddSkeletonExternalLogins(configuration);
        await using var provider = services.BuildServiceProvider();
        return (await provider.GetRequiredService<IAuthenticationSchemeProvider>().GetAllSchemesAsync()).Select(scheme => scheme.Name).ToArray();
    }
}
