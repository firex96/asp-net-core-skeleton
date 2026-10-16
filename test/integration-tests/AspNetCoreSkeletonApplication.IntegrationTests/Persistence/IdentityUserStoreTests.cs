using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Persistence;

/// <summary>Identity 저장소가 NoTracking 기본값 아래에서도 사용자 토큰을 고쳐 쓰는지 — 다른 scope(요청)에서 다시 읽어 확인한다.</summary>
[Collection(InfrastructureCollection.Name)]
public class IdentityUserStoreTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "identity_user_store";

    private WebApplicationFactory<Program> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => infrastructure.ConfigureApp(builder, Database));
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task ReplacingAToken_IsSaved()
    {
        const string email = "token-owner@example.com";
        await WithUsersAsync(async users =>
        {
            var user = new ApplicationUser { UserName = email, Email = email };
            Assert.True((await users.CreateAsync(user)).Succeeded);
            Assert.True((await users.SetAuthenticationTokenAsync(user, "Test", "Value", "first")).Succeeded);
        });

        await WithUsersAsync(async users =>
        {
            var user = (await users.FindByEmailAsync(email))!;
            Assert.True((await users.SetAuthenticationTokenAsync(user, "Test", "Value", "second")).Succeeded);
        });

        await WithUsersAsync(async users =>
            Assert.Equal("second", await users.GetAuthenticationTokenAsync((await users.FindByEmailAsync(email))!, "Test", "Value")));
    }

    private async Task WithUsersAsync(Func<UserManager<ApplicationUser>, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
    }
}
