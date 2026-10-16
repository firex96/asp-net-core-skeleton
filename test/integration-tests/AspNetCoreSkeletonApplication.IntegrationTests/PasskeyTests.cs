using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// 패스키 API 의 서버 쪽 — 옵션 JSON(RP ID 는 <c>App:PublicOrigin</c> 의 호스트), 잘못된 세리머니 결과의 거절, 관리 API.
/// 실제 등록 · 로그인 세리머니는 브라우저 테스트(가상 인증기)가 본다.
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class PasskeyTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "passkey";

    private WebApplicationFactory<Program> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => infrastructure.ConfigureApp(builder, Database));
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task LoginOptions_AreForThisSite_AndNameNoUser()
    {
        var response = await _factory.CreateClient(TestUsers.Https).PostAsync("/api/auth/passkey/options", null, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var options = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(new Uri(InfrastructureFixture.PublicOrigin).Host, options.GetProperty("rpId").GetString());
        Assert.False(string.IsNullOrEmpty(options.GetProperty("challenge").GetString()));
        Assert.True(!options.TryGetProperty("allowCredentials", out var allowed) || allowed.GetArrayLength() == 0);
    }

    [Fact]
    public async Task Login_WithABrokenCredential_Is401()
    {
        var client = _factory.CreateClient(TestUsers.Https);
        await client.PostAsync("/api/auth/passkey/options", null, Token);

        var response = await client.PostAsJsonAsync("/api/auth/passkey/login", new { credential = "{\"id\":\"AAAA\",\"type\":\"public-key\"}" }, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me", Token)).StatusCode);
    }

    [Fact]
    public async Task Management_CreatesOptionsForTheUser_AndRejectsBrokenOrUnknownPasskeys()
    {
        var (client, userId) = await TestUsers.SignInAsync(_factory, "passkey-manager@example.com", administrator: false);

        var options = await (await client.PostAsync("/api/account/passkeys/options", null, Token)).Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(new Uri(InfrastructureFixture.PublicOrigin).Host, options.GetProperty("rp").GetProperty("id").GetString());
        Assert.Equal("passkey-manager@example.com", options.GetProperty("user").GetProperty("name").GetString());

        var broken = await client.PostAsJsonAsync("/api/account/passkeys", new { credential = "{\"id\":\"AAAA\",\"type\":\"public-key\"}", name = "노트북" }, Token);
        Assert.Equal(HttpStatusCode.BadRequest, broken.StatusCode);

        Assert.Empty((await client.GetFromJsonAsync<JsonElement>("/api/account/passkeys", Token)).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/account/passkeys/AAAA", Token)).StatusCode);
        Assert.True(userId > 0);
    }

    [Fact]
    public async Task Management_NeedsASession()
    {
        var response = await _factory.CreateClient(TestUsers.Https).GetAsync("/api/account/passkeys", Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;
}
