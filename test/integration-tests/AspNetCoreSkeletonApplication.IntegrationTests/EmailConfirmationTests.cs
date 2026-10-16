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
/// 이메일 확인을 실제 메일(Mailpit)로 끝까지 — 가입하면 확인 링크가 오고, 확인하기 전에는 로그인할 수 없다. 확인 전 계정이라는 사실은
/// 비밀번호가 맞을 때만 알려 주고(틀리면 다른 계정과 같은 401), 다시 보내기는 언제나 202 다.
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class EmailConfirmationTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "email_confirmation";

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
    public async Task Register_MailsALink_AndLoginWaitsForTheConfirmation()
    {
        const string email = "newcomer@example.com";
        var client = _factory.CreateClient(TestUsers.Https);

        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(email)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me", Token)).StatusCode);

        var unconfirmed = await LoginAsync(client, email, TestUsers.Password);
        Assert.Equal(HttpStatusCode.Forbidden, unconfirmed.StatusCode);
        Assert.Equal("EMAIL_NOT_CONFIRMED_ERROR", await CodeOfAsync(unconfirmed));
        var wrong = await LoginAsync(client, email, "Wr0ng-Passw0rd!");
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);

        var link = await ReadLinkAsync(email);
        Assert.Equal($"{InfrastructureFixture.PublicOrigin}/confirm-email", link.GetLeftPart(UriPartial.Path));
        var query = HttpUtility.ParseQueryString(link.Query);
        var confirm = await client.PostAsJsonAsync("/api/auth/email/confirm", new { userId = long.Parse(query["userId"]!, System.Globalization.CultureInfo.InvariantCulture), code = query["code"] }, Token);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me", Token)).StatusCode); // 확인이 로그인시키지는 않는다

        Assert.Equal(HttpStatusCode.NoContent, (await LoginAsync(client, email, TestUsers.Password)).StatusCode);
    }

    [Fact]
    public async Task Confirm_WithAWrongCodeOrUser_Is400InvalidToken()
    {
        const string email = "mistyped@example.com";
        await RegisterAsync(email);
        var userId = HttpUtility.ParseQueryString((await ReadLinkAsync(email)).Query)["userId"];
        var client = _factory.CreateClient(TestUsers.Https);

        var wrongCode = await client.PostAsJsonAsync("/api/auth/email/confirm", new { userId = long.Parse(userId!, System.Globalization.CultureInfo.InvariantCulture), code = "bm90LWEtdG9rZW4" }, Token);
        var notBase64 = await client.PostAsJsonAsync("/api/auth/email/confirm", new { userId = long.Parse(userId!, System.Globalization.CultureInfo.InvariantCulture), code = "%%%" }, Token);
        var unknownUser = await client.PostAsJsonAsync("/api/auth/email/confirm", new { userId = 999999, code = "bm90LWEtdG9rZW4" }, Token);

        Assert.All([wrongCode, notBase64, unknownUser], response => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode));
        Assert.Equal("INVALID_TOKEN_ERROR", await CodeOfAsync(wrongCode));
        Assert.Equal("INVALID_TOKEN_ERROR", await CodeOfAsync(unknownUser));
    }

    [Fact]
    public async Task Resend_LooksTheSame_ButMailsOnlyUnconfirmedAccounts()
    {
        const string unconfirmed = "waiting@example.com";
        const string confirmed = "done@example.com";
        const string unknown = "nobody@example.com";
        await RegisterAsync(unconfirmed);
        await TestUsers.SignInAsync(_factory, confirmed, administrator: false);

        var responses = new List<HttpResponseMessage>();
        foreach (var email in new[] { unconfirmed, confirmed, unknown })
        {
            responses.Add(await _factory.CreateClient(TestUsers.Https).PostAsJsonAsync("/api/auth/email/resend", new { email }, Token));
        }

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Accepted, response.StatusCode));
        Assert.Equal(2, await CountOutboxMailsToAsync(unconfirmed));
        Assert.Equal(1, await CountOutboxMailsToAsync(confirmed)); // 가입할 때 한 통뿐
        Assert.Equal(0, await CountOutboxMailsToAsync(unknown));
    }

    [Fact]
    public async Task WrongPasswords_StillLockAnUnconfirmedAccount()
    {
        const string email = "unconfirmed-locked@example.com";
        await RegisterAsync(email);
        var client = _factory.CreateClient(TestUsers.Https);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await LoginAsync(client, email, "Wr0ng-Passw0rd!");
        }

        var right = await LoginAsync(client, email, TestUsers.Password);
        Assert.Equal((HttpStatusCode)423, right.StatusCode);
        Assert.Equal("ACCOUNT_LOCKED_ERROR", await CodeOfAsync(right));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private Task<HttpResponseMessage> RegisterAsync(string email) =>
        _factory.CreateClient(TestUsers.Https).PostAsJsonAsync("/api/auth/register", new { email, password = TestUsers.Password }, Token);

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password }, Token);

    private static async Task<string?> CodeOfAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("code").GetString();

    /// <summary>받은 최신 메일 본문의 링크. 아웃박스를 거쳐 오므로 올 때까지 기다린다.</summary>
    private async Task<Uri> ReadLinkAsync(string email)
    {
        var query = Uri.EscapeDataString($"to:\"{email}\"");
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var search = await _mailpit.GetFromJsonAsync<JsonElement>($"api/v1/search?query={query}", Token);
            if (search.GetProperty("messages").GetArrayLength() > 0)
            {
                var id = search.GetProperty("messages")[0].GetProperty("ID").GetString();
                var message = await _mailpit.GetFromJsonAsync<JsonElement>($"api/v1/message/{id}", Token);
                return new Uri(message.GetProperty("Text").GetString()!.Split('\n').Select(part => part.Trim())
                    .Single(part => part.StartsWith("https://", StringComparison.Ordinal)));
            }

            await Task.Delay(200, Token);
        }

        throw new TimeoutException($"{email} 에게 온 메일이 없다");
    }

    private async Task<int> CountOutboxMailsToAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().MailOutbox.CountAsync(mail => mail.Recipient == email, Token);
    }
}
