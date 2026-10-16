using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// 클라이언트 구성 동기화 — 구성에서 빠진 클라이언트는 지우지 않고 권한을 비워 사용을 멈추고, 다시 넣으면 다시 켜진다.
/// 같은 DB 에 구성을 바꿔 가며 앱을 다시 띄운다(재배포).
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class OAuthClientSyncTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "oauth_client_sync";
    private const string Secret = "sync-service-secret";

    public async ValueTask InitializeAsync() => await infrastructure.RecreateDatabaseAsync(Database);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task RemovedClient_IsDisabledNotDeleted_AndComesBackWhenConfiguredAgain()
    {
        await using (var both = Start(includeService: true))
        {
            Assert.Equal(HttpStatusCode.OK, (await ServiceTokenAsync(both)).StatusCode);
        }

        await using (var keeperOnly = Start(includeService: false))
        {
            var response = await ServiceTokenAsync(keeperOnly);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("unauthorized_client", (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetString());

            using var scope = keeperOnly.Services.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            var service = await manager.FindByClientIdAsync("sync-service", Token);
            Assert.NotNull(service);
            Assert.Empty(await manager.GetPermissionsAsync(service, Token));
            Assert.NotEmpty(await manager.GetPermissionsAsync((await manager.FindByClientIdAsync("sync-keeper", Token))!, Token));
        }

        await using var again = Start(includeService: true);
        Assert.Equal(HttpStatusCode.OK, (await ServiceTokenAsync(again)).StatusCode);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private WebApplicationFactory<Program> Start(bool includeService)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            infrastructure.ConfigureApp(builder, Database);
            OAuthFlows.RegisterClient(builder, 0, "sync-keeper", consent: "Implicit", grants: ["client_credentials"], scopes: ["products.manage"], secret: Secret);
            if (includeService)
            {
                OAuthFlows.RegisterClient(builder, 1, "sync-service", consent: "Implicit", grants: ["client_credentials"], scopes: ["products.manage"], secret: Secret);
            }
        });
        _ = factory.Services; // 기동 — 동기화가 돈다
        return factory;
    }

    private static Task<HttpResponseMessage> ServiceTokenAsync(WebApplicationFactory<Program> factory) =>
        OAuthFlows.PostTokenAsync(factory.CreateClient(TestUsers.Https), new()
        {
            ["grant_type"] = "client_credentials", ["client_id"] = "sync-service", ["client_secret"] = Secret, ["scope"] = "products.manage",
        });
}
