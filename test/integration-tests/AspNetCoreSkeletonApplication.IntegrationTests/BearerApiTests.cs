using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// 이 서버가 발급한 Bearer 토큰으로 API 를 부른다 — 사용자 토큰의 실효 권한은 사용자 권한 ∩ 토큰 scope, 클라이언트 토큰은 scope 그대로.
/// 감사 주체는 사용자 토큰이면 그 사용자, 클라이언트 토큰이면 시스템(0)이다.
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class BearerApiTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "bearer_api";
    private const string ServiceSecret = "bearer-service-secret";

    private WebApplicationFactory<Program> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            infrastructure.ConfigureApp(builder, Database);
            OAuthFlows.RegisterClient(builder, 0, "app", consent: "Implicit", grants: ["authorization_code"],
                scopes: ["openid", "email", "products.manage", "authorization.manage"]);
            OAuthFlows.RegisterClient(builder, 1, "service", consent: "Implicit", grants: ["client_credentials"],
                scopes: ["products.manage"], secret: ServiceSecret);
        });
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task ClientToken_WithTheScope_CanManageProducts_AsTheSystem()
    {
        var productId = await CreateProductAsCookieUserAsync("owner@example.com");
        var token = await ClientTokenAsync("products.manage");

        var changed = await SendAsync(HttpMethod.Put, $"/api/products/{productId}/status", token, new { status = "OnSale" });

        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(0, (await FindProductAsync(productId)).UpdatedBy);
    }

    [Fact]
    public async Task ClientToken_CannotUseWhatItsScopeDoesNotCover()
    {
        var token = await ClientTokenAsync("products.manage");

        var groups = await SendAsync(HttpMethod.Get, "/api/authorization/groups", token);
        var me = await SendAsync(HttpMethod.Get, "/api/auth/me", token);

        Assert.Equal(HttpStatusCode.Forbidden, groups.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, me.StatusCode); // 사용자가 없다
    }

    [Fact]
    public async Task UserToken_IsLimitedToTheGrantedScope_EvenForAnAdministrator()
    {
        var (browser, _) = await SignInAsync("admin@example.com", administrator: true);
        var productId = await CreateProductAsCookieUserAsync("maker@example.com");
        var narrow = await OAuthFlows.UserAccessTokenAsync(browser, NewClient(), "app", "openid email");

        var changed = await SendAsync(HttpMethod.Put, $"/api/products/{productId}/status", narrow, new { status = "OnSale" });
        var me = await (await SendAsync(HttpMethod.Get, "/api/auth/me", narrow)).Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(HttpStatusCode.Forbidden, changed.StatusCode);
        Assert.Empty(me.GetProperty("permissions").EnumerateArray());
    }

    [Fact]
    public async Task UserToken_SeesOthersDrafts_OnlyWithTheManageScope()
    {
        var (browser, _) = await SignInAsync("draft-admin@example.com", administrator: true);
        var draftId = await CreateProductAsCookieUserAsync("draft-maker@example.com");
        var narrow = await OAuthFlows.UserAccessTokenAsync(browser, NewClient(), "app", "openid email");
        var managing = await OAuthFlows.UserAccessTokenAsync(browser, NewClient(), "app", "openid products.manage");

        // 관리자라도 토큰 scope 에 products.manage 가 없으면 남의 판매 전 상품은 없는 것과 같다.
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, $"/api/products/{draftId}", narrow)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, $"/api/products/{draftId}", managing)).StatusCode);
    }

    [Fact]
    public async Task UserToken_WithTheScope_ActsAsTheUser()
    {
        var (browser, adminId) = await SignInAsync("acting-admin@example.com", administrator: true);
        var productId = await CreateProductAsCookieUserAsync("maker2@example.com");
        var token = await OAuthFlows.UserAccessTokenAsync(browser, NewClient(), "app", "openid products.manage");

        var changed = await SendAsync(HttpMethod.Put, $"/api/products/{productId}/status", token, new { status = "OnSale" });
        var me = await (await SendAsync(HttpMethod.Get, "/api/auth/me", token)).Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(adminId, (await FindProductAsync(productId)).UpdatedBy);
        Assert.Equal(adminId, me.GetProperty("id").GetInt64());
        Assert.Equal(["products.manage"], me.GetProperty("permissions").EnumerateArray().Select(code => code.GetString()));
    }

    [Fact]
    public async Task UserToken_CannotExceedTheUsersOwnPermissions()
    {
        var (browser, _) = await SignInAsync("plain@example.com", administrator: false);
        var productId = await CreateProductAsCookieUserAsync("maker3@example.com");
        var token = await OAuthFlows.UserAccessTokenAsync(browser, NewClient(), "app", "openid products.manage");

        var changed = await SendAsync(HttpMethod.Put, $"/api/products/{productId}/status", token, new { status = "OnSale" });

        Assert.Equal(HttpStatusCode.Forbidden, changed.StatusCode);
    }

    [Fact]
    public async Task MissingOrInvalidBearer_Is401()
    {
        var invalid = await SendAsync(HttpMethod.Post, "/api/products", "not-a-token", new { name = "X", price = 1 });
        var none = await NewClient().PostAsJsonAsync("/api/products", new { name = "X", price = 1 }, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, none.StatusCode);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = TestUsers.Https.BaseAddress, AllowAutoRedirect = false });

    private async Task<(HttpClient Browser, long UserId)> SignInAsync(string email, bool administrator)
    {
        var (registered, userId) = await TestUsers.SignInAsync(_factory, email, administrator);
        registered.Dispose();
        var browser = NewClient();
        (await browser.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password }, Token)).EnsureSuccessStatusCode();
        return (browser, userId);
    }

    private async Task<long> CreateProductAsCookieUserAsync(string email)
    {
        var (client, _) = await TestUsers.SignInAsync(_factory, email, administrator: false);
        var created = await client.PostAsJsonAsync("/api/products", new { name = "Lamp", price = 100 }, Token);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetInt64();
    }

    private async Task<string> ClientTokenAsync(string scope) =>
        (await OAuthFlows.TokenAsync(NewClient(), new()
        {
            ["grant_type"] = "client_credentials", ["client_id"] = "service", ["client_secret"] = ServiceSecret, ["scope"] = scope,
        })).GetProperty("access_token").GetString()!;

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return NewClient().SendAsync(request, Token);
    }

    private async Task<Common.Product.Persistence.Entities.ProductEntity> FindProductAsync(long id)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Products.SingleAsync(product => product.Id == id, Token);
    }
}
