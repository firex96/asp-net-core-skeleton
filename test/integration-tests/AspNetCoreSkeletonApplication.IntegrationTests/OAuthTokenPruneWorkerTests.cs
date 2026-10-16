using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// Batch 없이 배포한 앱의 토큰 정리 — <c>TokenPruning:RunInServer</c> 면 Server 가 기동하자마자 정리한다(Batch Job 과 같은 규칙).
/// 기본(false)이면 손대지 않는다.
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class OAuthTokenPruneWorkerTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "oauth_prune_worker";

    public async ValueTask InitializeAsync() => await infrastructure.RecreateDatabaseAsync(Database);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task RunInServer_PrunesOldUnusableTokens_AndTheDefaultLeavesThem()
    {
        var now = DateTimeOffset.UtcNow;
        string expiredOld, usable;
        await using (var idle = Start(runInServer: false))
        {
            expiredOld = await SeedTokenAsync(idle, Statuses.Valid, created: now.AddDays(-30), expires: now.AddDays(-29));
            usable = await SeedTokenAsync(idle, Statuses.Valid, created: now.AddDays(-30), expires: now.AddDays(1));
            await Task.Delay(500, Token);
            Assert.Contains(expiredOld, await RemainingTokenIdsAsync(idle));
        }

        await using var pruning = Start(runInServer: true);
        for (var attempt = 0; attempt < 50 && (await RemainingTokenIdsAsync(pruning)).Contains(expiredOld); attempt++)
        {
            await Task.Delay(200, Token);
        }

        var remaining = await RemainingTokenIdsAsync(pruning);
        Assert.DoesNotContain(expiredOld, remaining);
        Assert.Contains(usable, remaining);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private WebApplicationFactory<Program> Start(bool runInServer)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            infrastructure.ConfigureApp(builder, Database);
            builder.UseSetting("Authentication:OAuthServer:TokenPruning:RunInServer", runInServer ? "true" : "false");
        });
        _ = factory.Services;
        return factory;
    }

    private static async Task<string> SeedTokenAsync(WebApplicationFactory<Program> factory, string status, DateTimeOffset created, DateTimeOffset expires)
    {
        using var scope = factory.Services.CreateScope();
        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var application = await applications.FindByClientIdAsync("prune-worker-test", Token)
                          ?? await applications.CreateAsync(new OpenIddictApplicationDescriptor { ClientId = "prune-worker-test", ClientType = ClientTypes.Public }, Token);
        var tokens = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var token = await tokens.CreateAsync(new OpenIddictTokenDescriptor
        {
            ApplicationId = await applications.GetIdAsync(application, Token),
            CreationDate = created,
            ExpirationDate = expires,
            Status = status,
            Subject = "1",
            Type = TokenTypeHints.AccessToken,
        }, Token);
        return (await tokens.GetIdAsync(token, Token))!;
    }

    private static async Task<List<string>> RemainingTokenIdsAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        List<string> ids = [];
        await foreach (var token in tokens.ListAsync(count: null, offset: null, Token))
        {
            ids.Add((await tokens.GetIdAsync(token, Token))!);
        }

        return ids;
    }
}
