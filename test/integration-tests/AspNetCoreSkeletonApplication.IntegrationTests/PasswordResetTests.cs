using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// 비밀번호 재설정을 실제 메일(Mailpit)로 끝까지 — 요청 → 메일의 링크 → 재설정 → 새 비밀번호로 로그인. 토큰은 한 번만 쓰이고,
/// 응답으로 가입 여부를 알 수 없다.
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class PasswordResetTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "password_reset";
    private const string NewPassword = "N3w-Passw0rd!-long";

    private WebApplicationFactory<Program> _factory = null!;
    private readonly HttpClient _mailpit = new() { BaseAddress = infrastructure.MailpitApi };

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => infrastructure.ConfigureApp(builder, Database));
    }

    public async ValueTask DisposeAsync()
    {
        _mailpit.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ResetThroughTheMailedLink_ChangesThePasswordOnce()
    {
        const string email = "forgetful@example.com";
        await TestUsers.SignInAsync(_factory, email, administrator: false);
        var client = _factory.CreateClient(TestUsers.Https);

        var forgot = await client.PostAsJsonAsync("/api/auth/password/forgot", new { email }, Token);
        Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);

        var link = await ReadResetLinkAsync(email);
        Assert.Equal($"{InfrastructureFixture.PublicOrigin}/reset-password", link.GetLeftPart(UriPartial.Path));
        var query = HttpUtility.ParseQueryString(link.Query);
        Assert.Equal(email, query["email"]);

        var reset = await client.PostAsJsonAsync("/api/auth/password/reset", new { email, code = query["code"], newPassword = NewPassword }, Token);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await LoginAsync(email, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, TestUsers.Password)).StatusCode);

        var reused = await client.PostAsJsonAsync("/api/auth/password/reset", new { email, code = query["code"], newPassword = "An0ther-Passw0rd!" }, Token);
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
        Assert.Equal("INVALID_TOKEN_ERROR", (await reused.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Forgot_ForUnknownEmail_LooksTheSame_AndSendsNothing()
    {
        const string email = "nobody@example.com";

        var forgot = await _factory.CreateClient(TestUsers.Https).PostAsJsonAsync("/api/auth/password/forgot", new { email }, Token);

        Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);
        Assert.Equal(0, await CountOutboxMailsToAsync(email));
    }

    [Fact]
    public async Task Reset_WithWeakPassword_Is400NotValid_AndKeepsTheToken()
    {
        const string email = "weak@example.com";
        await TestUsers.SignInAsync(_factory, email, administrator: false);
        var client = _factory.CreateClient(TestUsers.Https);
        await client.PostAsJsonAsync("/api/auth/password/forgot", new { email }, Token);
        var code = HttpUtility.ParseQueryString((await ReadResetLinkAsync(email)).Query)["code"];

        var weak = await client.PostAsJsonAsync("/api/auth/password/reset", new { email, code, newPassword = "short" }, Token);
        var strong = await client.PostAsJsonAsync("/api/auth/password/reset", new { email, code, newPassword = NewPassword }, Token);

        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        Assert.Equal("NOT_VALID_ERROR", (await weak.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.NoContent, strong.StatusCode);
    }

    [Fact]
    public async Task Reset_UnlocksALockedAccount()
    {
        const string email = "locked-out@example.com";
        await TestUsers.SignInAsync(_factory, email, administrator: false);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await LoginAsync(email, "wrong-password");
        }

        Assert.Equal((HttpStatusCode)423, (await LoginAsync(email, TestUsers.Password)).StatusCode);

        var client = _factory.CreateClient(TestUsers.Https);
        await client.PostAsJsonAsync("/api/auth/password/forgot", new { email }, Token);
        var code = HttpUtility.ParseQueryString((await ReadResetLinkAsync(email)).Query)["code"];
        await client.PostAsJsonAsync("/api/auth/password/reset", new { email, code, newPassword = NewPassword }, Token);

        Assert.Equal(HttpStatusCode.NoContent, (await LoginAsync(email, NewPassword)).StatusCode);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _factory.CreateClient(TestUsers.Https).PostAsJsonAsync("/api/auth/login", new { email, password }, Token);

    /// <summary>받은 최신 메일의 본문에서 재설정 링크를 꺼낸다. 메일은 아웃박스를 거쳐 백그라운드가 보내므로 올 때까지 기다린다.</summary>
    private async Task<Uri> ReadResetLinkAsync(string email)
    {
        var search = await _mailpit.GetFromJsonAsync<JsonElement>($"api/v1/search?query={Uri.EscapeDataString($"to:\"{email}\"")}", Token);
        for (var attempt = 0; attempt < 50 && search.GetProperty("messages").GetArrayLength() == 0; attempt++)
        {
            await Task.Delay(200, Token);
            search = await _mailpit.GetFromJsonAsync<JsonElement>($"api/v1/search?query={Uri.EscapeDataString($"to:\"{email}\"")}", Token);
        }

        var id = search.GetProperty("messages")[0].GetProperty("ID").GetString();
        var message = await _mailpit.GetFromJsonAsync<JsonElement>($"api/v1/message/{id}", Token);
        var text = message.GetProperty("Text").GetString()!;
        var line = text.Split('\n').Select(part => part.Trim()).Single(part => part.StartsWith("https://", StringComparison.Ordinal));
        return new Uri(line);
    }

    /// <summary>아웃박스에 들어간 메일 수 — 발송을 기다리지 않고 "보내지 않았다" 를 확인한다.</summary>
    private async Task<int> CountOutboxMailsToAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().MailOutbox.CountAsync(mail => mail.Recipient == email, Token);
    }
}
