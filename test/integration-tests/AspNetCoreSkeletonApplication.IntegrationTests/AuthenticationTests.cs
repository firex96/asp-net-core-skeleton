using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>Identity + 쿠키 — 가입 · 로그인 · 잠금, 로그인한 사용자가 감사 주체, Redis 의 Data Protection 키로 인스턴스끼리 쿠키를 읽는다.</summary>
[Collection(InfrastructureCollection.Name)]
public class AuthenticationTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "authentication";
    private const string Password = "Passw0rd!-long";
    private static readonly WebApplicationFactoryClientOptions Https = new() { BaseAddress = new Uri("https://localhost") };

    private WebApplicationFactory<Program> _first = null!;
    private WebApplicationFactory<Program> _second = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _first = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => infrastructure.ConfigureApp(builder, Database));
        _second = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => infrastructure.ConfigureApp(builder, Database));
    }

    public async ValueTask DisposeAsync()
    {
        await _second.DisposeAsync();
        await _first.DisposeAsync();
    }

    [Fact]
    public async Task RegisterLoginMeLogout()
    {
        var client = _first.CreateClient(Https);

        Assert.Equal(HttpStatusCode.Created, (await PostAsync(client, "/api/auth/register", "me@example.com", Password)).StatusCode);
        var login = await PostAsync(client, "/api/auth/login", "me@example.com", Password);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), value => value.StartsWith("__Host-session=", StringComparison.Ordinal));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var me = await ReadJsonAsync(await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken));
        Assert.Equal("me@example.com", me.GetProperty("email").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null, TestContext.Current.CancellationToken)).StatusCode);
        var afterLogout = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
        Assert.Equal("UNAUTHORIZED_ERROR", (await ReadJsonAsync(afterLogout)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task WeakPassword_Is400NotValid()
    {
        var response = await PostAsync(_first.CreateClient(Https), "/api/auth/register", "weak@example.com", "short");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("NOT_VALID_ERROR", (await ReadJsonAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task FiveWrongPasswords_LockTheAccount()
    {
        var client = _first.CreateClient(Https);
        await PostAsync(client, "/api/auth/register", "locked@example.com", Password);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var wrong = await PostAsync(client, "/api/auth/login", "locked@example.com", "wrong-password");
            Assert.True(wrong.StatusCode is HttpStatusCode.Unauthorized or (HttpStatusCode)423);
        }

        var right = await PostAsync(client, "/api/auth/login", "locked@example.com", Password);
        Assert.Equal((HttpStatusCode)423, right.StatusCode);
        Assert.Equal("ACCOUNT_LOCKED_ERROR", (await ReadJsonAsync(right)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task LoggedInUser_IsTheAuditor()
    {
        var client = _first.CreateClient(Https);
        await PostAsync(client, "/api/auth/register", "author@example.com", Password);
        await PostAsync(client, "/api/auth/login", "author@example.com", Password);
        var userId = (await ReadJsonAsync(await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken))).GetProperty("id").GetInt64();

        var created = await ReadJsonAsync(await client.PostAsJsonAsync(
            "/api/products", new { name = "Signed", price = 1000 }, TestContext.Current.CancellationToken));

        using var scope = _first.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = await dbContext.Products.SingleAsync(row => row.Id == created.GetProperty("id").GetInt64(), TestContext.Current.CancellationToken);
        Assert.Equal(userId, product.CreatedBy);
    }

    [Fact]
    public async Task CookieFromOneInstance_IsReadByAnother()
    {
        var first = _first.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = Https.BaseAddress, HandleCookies = false });
        await PostAsync(first, "/api/auth/register", "roaming@example.com", Password);
        var login = await PostAsync(first, "/api/auth/login", "roaming@example.com", Password);
        var cookie = login.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Host-session=", StringComparison.Ordinal)).Split(';')[0];

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Cookie", cookie);
        var response = await _second.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = Https.BaseAddress, HandleCookies = false })
            .SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string path, string email, string password) =>
        client.PostAsJsonAsync(path, new { email, password }, TestContext.Current.CancellationToken);

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
}
