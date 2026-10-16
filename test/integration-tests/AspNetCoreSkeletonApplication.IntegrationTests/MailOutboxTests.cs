using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Common.Mail.Dtos;
using AspNetCoreSkeletonApplication.Common.Mail.Persistence.Services;
using AspNetCoreSkeletonApplication.Common.Mail.Services.Biz;
using AspNetCoreSkeletonApplication.Common.Persistence;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// 메일 아웃박스를 실제 DB · SMTP(Mailpit)로 — 백그라운드 발송, 보낸 뒤 본문 비우기, SMTP 장애 때 다시 시도 예약,
/// 여러 인스턴스가 동시에 잡아도 한 통은 한 곳만 가져가는지(UPDLOCK · READPAST).
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class MailOutboxTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "mail_outbox";

    private readonly HttpClient _mailpit = new() { BaseAddress = infrastructure.MailpitApi };
    private WebApplicationFactory<Program>? _factory;

    public async ValueTask InitializeAsync() => await infrastructure.RecreateDatabaseAsync(Database);

    public async ValueTask DisposeAsync()
    {
        _mailpit.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task EnqueuedMail_IsSentInTheBackground_AndItsBodyCleared()
    {
        const string email = "outbox-delivered@example.com";
        var factory = Start();

        await EnqueueAsync(factory, email);

        await WaitUntilAsync(async () => await CountMessagesToAsync(email) == 1);
        var row = await WaitForRowAsync(factory, email, mail => mail.SentAt is not null);
        Assert.Equal(1, row.Attempts);
        Assert.Null(row.TextBody);
        Assert.Null(row.HtmlBody);
        Assert.Null(row.LastError);
    }

    [Fact]
    public async Task SmtpDown_SchedulesARetry_AndKeepsTheBody()
    {
        const string email = "outbox-retry@example.com";
        var factory = Start(builder => builder.UseSetting("Mail:Smtp:Port", ClosedPort().ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var before = DateTimeOffset.UtcNow;

        await EnqueueAsync(factory, email);

        var row = await WaitForRowAsync(factory, email, mail => mail.LastError is not null);
        Assert.Equal(1, row.Attempts);
        Assert.Null(row.SentAt);
        Assert.Null(row.FailedAt);
        Assert.True(row.NextAttemptAt >= before.AddSeconds(30), $"다음 시도 {row.NextAttemptAt:O} 는 30초 뒤 이후여야 한다");
        Assert.NotNull(row.TextBody);
    }

    [Fact]
    public async Task ConcurrentClaims_NeverTakeTheSameMail()
    {
        // 발송 루프가 끼어들지 않게 길게 쉬게 하고, 두 "인스턴스"(scope · 연결)가 직접 동시에 잡는다.
        var factory = Start(builder => builder.UseSetting("Mail:Outbox:PollInterval", "01:00:00"));
        for (var index = 0; index < 20; index++)
        {
            await EnqueueAsync(factory, $"outbox-claim-{index}@example.com");
        }

        var now = DateTimeOffset.UtcNow.AddSeconds(1);
        var claims = await Task.WhenAll(ClaimAsync(factory, now), ClaimAsync(factory, now));

        var ids = claims.SelectMany(claim => claim).Select(mail => mail.Id).ToList();
        Assert.Equal(20, ids.Count);
        Assert.Equal(20, ids.Distinct().Count());
        Assert.Empty(await ClaimAsync(factory, now));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private WebApplicationFactory<Program> Start(Action<IWebHostBuilder>? configure = null)
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            infrastructure.ConfigureApp(builder, Database);
            configure?.Invoke(builder);
        });
        _ = _factory.Services; // 기동 — 발송 루프가 돈다
        return _factory;
    }

    private static async Task EnqueueAsync(WebApplicationFactory<Program> factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<MailOutboxBizService>()
            .EnqueueAsync(new OutgoingMailDto(email, "아웃박스 시험", $"https://spa.test/hello?to={email}", "<p>안녕하세요</p>"), Token);
    }

    private static async Task<IReadOnlyList<MailOutboxDto>> ClaimAsync(WebApplicationFactory<Program> factory, DateTimeOffset now)
    {
        using var scope = factory.Services.CreateScope();
        var transaction = scope.ServiceProvider.GetRequiredService<IMainTransaction>();
        var outbox = scope.ServiceProvider.GetRequiredService<IMailOutboxEntityService>();
        return await transaction.ExecuteAsync(token => outbox.ClaimDueAsync(15, now, now.AddMinutes(5), token), Token);
    }

    private static async Task<Common.Mail.Persistence.Entities.MailOutboxEntity> WaitForRowAsync(
        WebApplicationFactory<Program> factory, string email, Func<Common.Mail.Persistence.Entities.MailOutboxEntity, bool> condition)
    {
        Common.Mail.Persistence.Entities.MailOutboxEntity? row = null;
        await WaitUntilAsync(async () =>
        {
            using var scope = factory.Services.CreateScope();
            row = await scope.ServiceProvider.GetRequiredService<AppDbContext>().MailOutbox.SingleAsync(mail => mail.Recipient == email, Token);
            return condition(row);
        });
        return row!;
    }

    private async Task<int> CountMessagesToAsync(string email) =>
        (await _mailpit.GetFromJsonAsync<JsonElement>($"api/v1/search?query={Uri.EscapeDataString($"to:\"{email}\"")}", Token))
            .GetProperty("messages").GetArrayLength();

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(200, Token);
        }

        Assert.Fail("10초 안에 조건을 만족하지 않았다");
    }

    /// <summary>아무도 듣지 않는 포트 — SMTP 연결이 거절된다.</summary>
    private static int ClosedPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
