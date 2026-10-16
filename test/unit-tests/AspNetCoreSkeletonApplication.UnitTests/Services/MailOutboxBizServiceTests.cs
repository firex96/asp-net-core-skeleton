using AspNetCoreSkeletonApplication.Common.Mail;
using AspNetCoreSkeletonApplication.Common.Mail.Clients;
using AspNetCoreSkeletonApplication.Common.Mail.Domain;
using AspNetCoreSkeletonApplication.Common.Mail.Dtos;
using AspNetCoreSkeletonApplication.Common.Mail.Persistence.Services;
using AspNetCoreSkeletonApplication.Common.Mail.Services.Biz;
using AspNetCoreSkeletonApplication.Common.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AspNetCoreSkeletonApplication.UnitTests.Services;

/// <summary>발송 순서 — 잡기(트랜잭션) → SMTP(트랜잭션 밖) → 결과 기록(트랜잭션). 잡는 쿼리 · 잠금은 통합 테스트가 본다.</summary>
public class MailOutboxBizServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 3, 30, 0, TimeSpan.Zero);

    private readonly IMailOutboxEntityService _outbox = Substitute.For<IMailOutboxEntityService>();
    private readonly IMailClient _client = Substitute.For<IMailClient>();
    private readonly MailOutboxOptions _options = new() { BatchSize = 10, Lease = TimeSpan.FromMinutes(5) };

    private MailOutboxBizService CreateBizService() =>
        new(_outbox, _client, new InlineTransaction(), new FixedTimeProvider(Now), Options.Create(_options), NullLogger<MailOutboxBizService>.Instance);

    [Fact]
    public async Task Enqueue_StoresTheMailDueNow()
    {
        var mail = new OutgoingMailDto("user@example.com", "제목", "본문", "<p>본문</p>");

        await CreateBizService().EnqueueAsync(mail, TestContext.Current.CancellationToken);

        await _outbox.Received(1).CreateAsync(mail, Now, Arg.Any<CancellationToken>());
        await _client.DidNotReceiveWithAnyArgs().SendAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendDue_ClaimsWithALease_SendsAndMarksSent()
    {
        _outbox.ClaimDueAsync(10, Now, Now.AddMinutes(5), Arg.Any<CancellationToken>()).Returns([Claimed(1, attempts: 1)]);

        var claimed = await CreateBizService().SendDueAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, claimed);
        await _client.Received(1).SendAsync(new OutgoingMailDto("user@example.com", "제목", "본문", "<p>본문</p>"), Arg.Any<CancellationToken>());
        await _outbox.Received(1).MarkSentAsync(1, Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendDue_Failure_SchedulesARetry_AndGoesOnWithTheRest()
    {
        _outbox.ClaimDueAsync(10, Now, Now.AddMinutes(5), Arg.Any<CancellationToken>()).Returns([Claimed(1, attempts: 2), Claimed(2, attempts: 1)]);
        _client.SendAsync(Arg.Is<OutgoingMailDto>(mail => mail.Subject == "제목"), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("연결 끊김")), Task.CompletedTask);

        await CreateBizService().SendDueAsync(TestContext.Current.CancellationToken);

        await _outbox.Received(1).MarkFailedAsync(1, "IOException: 연결 끊김", MailRetryPolicy.NextAttemptAt(2, Now), Now, Arg.Any<CancellationToken>());
        await _outbox.Received(1).MarkSentAsync(2, Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendDue_LastAttemptFails_GivesUp()
    {
        _outbox.ClaimDueAsync(10, Now, Now.AddMinutes(5), Arg.Any<CancellationToken>()).Returns([Claimed(1, attempts: MailRetryPolicy.MaxAttempts)]);
        _client.SendAsync(Arg.Any<OutgoingMailDto>(), Arg.Any<CancellationToken>()).ThrowsAsync(new IOException("연결 끊김"));

        await CreateBizService().SendDueAsync(TestContext.Current.CancellationToken);

        await _outbox.Received(1).MarkFailedAsync(1, Arg.Any<string>(), null, Now, Arg.Any<CancellationToken>());
    }

    private static MailOutboxDto Claimed(long id, int attempts) => new(id, "user@example.com", "제목", "본문", "<p>본문</p>", attempts);

    private sealed class InlineTransaction : IMainTransaction
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken) => action(cancellationToken);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
