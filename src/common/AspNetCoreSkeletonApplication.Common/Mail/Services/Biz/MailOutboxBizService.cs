using AspNetCoreSkeletonApplication.Common.Mail.Clients;
using AspNetCoreSkeletonApplication.Common.Mail.Domain;
using AspNetCoreSkeletonApplication.Common.Mail.Dtos;
using AspNetCoreSkeletonApplication.Common.Mail.Persistence.Services;
using AspNetCoreSkeletonApplication.Common.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.Common.Mail.Services.Biz;

/// <summary>
/// 메일 아웃박스 — 요청은 <see cref="EnqueueAsync"/> 로 행만 넣고 끝나고(응답 시간이 SMTP 에 묶이지 않는다), 백그라운드가 <see cref="SendDueAsync"/> 로 보낸다.
/// SMTP 왕복은 트랜잭션 밖에서 한다. 보낸 뒤 기록하기 전에 멈추면 잡아 둔 기한이 지나 다시 보낸다 — 최소 한 번 전달이다.
/// </summary>
public partial class MailOutboxBizService(
    IMailOutboxEntityService outboxEntityService,
    IMailClient mailClient,
    IMainTransaction transaction,
    TimeProvider timeProvider,
    IOptions<MailOutboxOptions> options,
    ILogger<MailOutboxBizService> logger)
{
    public Task EnqueueAsync(OutgoingMailDto mail, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            await outboxEntityService.CreateAsync(mail, timeProvider.GetUtcNow(), token);
            return true;
        }, cancellationToken);

    /// <summary>때가 된 메일을 한 묶음 잡아 보낸다. 잡은 수를 돌려준다 — 묶음이 가득 찼으면 더 남아 있을 수 있다.</summary>
    public async Task<int> SendDueAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var claimed = await transaction.ExecuteAsync(
            token => outboxEntityService.ClaimDueAsync(options.Value.BatchSize, now, now + options.Value.Lease, token), cancellationToken);
        foreach (var mail in claimed)
        {
            await SendAsync(mail, cancellationToken);
        }

        return claimed.Count;
    }

    private async Task SendAsync(MailOutboxDto mail, CancellationToken cancellationToken)
    {
        try
        {
            await mailClient.SendAsync(new OutgoingMailDto(mail.Recipient, mail.Subject, mail.TextBody, mail.HtmlBody), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var now = timeProvider.GetUtcNow();
            var next = MailRetryPolicy.NextAttemptAt(mail.Attempts, now);
            await transaction.ExecuteAsync(async token =>
            {
                await outboxEntityService.MarkFailedAsync(mail.Id, $"{exception.GetType().Name}: {exception.Message}", next, now, token);
                return true;
            }, cancellationToken);
            if (next is null)
            {
                LogGaveUp(exception, mail.Id, mail.Attempts);
            }
            else
            {
                LogWillRetry(exception, mail.Id, mail.Attempts, next.Value);
            }

            return;
        }

        await transaction.ExecuteAsync(async token =>
        {
            await outboxEntityService.MarkSentAsync(mail.Id, timeProvider.GetUtcNow(), token);
            return true;
        }, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "메일 {MailId} 를 보내지 못했다({Attempts}번째) — {NextAttemptAt} 에 다시 보낸다")]
    private partial void LogWillRetry(Exception exception, long mailId, int attempts, DateTimeOffset nextAttemptAt);

    [LoggerMessage(Level = LogLevel.Error, Message = "메일 {MailId} 를 {Attempts}번 보내지 못해 포기했다")]
    private partial void LogGaveUp(Exception exception, long mailId, int attempts);
}
