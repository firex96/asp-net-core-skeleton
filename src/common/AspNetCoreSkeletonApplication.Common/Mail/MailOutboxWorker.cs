using AspNetCoreSkeletonApplication.Common.Mail.Services.Biz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCoreSkeletonApplication.Common.Mail;

/// <summary>
/// 아웃박스 발송 루프 — 묶음마다 새 scope(DbContext)로 <see cref="MailOutboxBizService.SendDueAsync"/> 를 부른다. 묶음이 가득 찼으면 바로 다음을,
/// 아니면 <see cref="MailOutboxOptions.PollInterval"/> 만큼 쉬고 다시 본다. 인스턴스마다 하나씩 돌아도 한 통은 한 번만 잡힌다.
/// </summary>
internal sealed partial class MailOutboxWorker(
    IServiceScopeFactory scopeFactory, IOptions<MailOutboxOptions> options, TimeProvider timeProvider, ILogger<MailOutboxWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            int claimed;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                claimed = await scope.ServiceProvider.GetRequiredService<MailOutboxBizService>().SendDueAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // DB 장애 등 — 루프를 멈추지 않는다. 다음 주기에 다시 본다.
                LogPollFailed(exception);
                claimed = 0;
            }

            if (claimed < options.Value.BatchSize)
            {
                try
                {
                    await Task.Delay(options.Value.PollInterval, timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "메일 아웃박스를 처리하지 못했다")]
    private partial void LogPollFailed(Exception exception);
}
