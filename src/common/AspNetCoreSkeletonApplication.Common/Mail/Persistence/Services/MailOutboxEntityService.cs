using AspNetCoreSkeletonApplication.Common.Mail.Dtos;
using AspNetCoreSkeletonApplication.Common.Mail.Persistence.Configurations;
using AspNetCoreSkeletonApplication.Common.Mail.Persistence.Entities;
using AspNetCoreSkeletonApplication.Common.Mail.Persistence.Mappers;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Mail.Persistence.Services;

internal sealed class MailOutboxEntityService(AppDbContext dbContext) : IMailOutboxEntityService
{
    public async Task CreateAsync(OutgoingMailDto mail, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var entity = mail.ToEntity();
        entity.NextAttemptAt = now;
        dbContext.MailOutbox.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 트랜잭션 안에서 부른다. <c>UPDLOCK</c> 이 잡은 행을 커밋까지 붙들고 <c>READPAST</c> 가 남이 붙든 행을 건너뛰므로,
    /// 여러 인스턴스가 동시에 잡아도 한 통은 한 곳만 가져간다. 커밋 뒤에는 잡아 둔 기한(<c>NextAttemptAt</c>)이 그 역할을 한다.
    /// </summary>
    public async Task<IReadOnlyList<MailOutboxDto>> ClaimDueAsync(
        int batchSize, DateTimeOffset now, DateTimeOffset leaseUntil, CancellationToken cancellationToken)
    {
        var claimed = await dbContext.MailOutbox
            .FromSql($"""
                SELECT TOP({batchSize}) * FROM mail_outbox WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE SentAt IS NULL AND FailedAt IS NULL AND NextAttemptAt <= {now}
                ORDER BY NextAttemptAt
                """)
            .AsTracking()
            .ToListAsync(cancellationToken);
        foreach (var mail in claimed)
        {
            mail.Attempts++;
            mail.NextAttemptAt = leaseUntil;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return claimed.Select(mail => mail.ToDto()).ToList();
    }

    public async Task MarkSentAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var mail = await FindTrackedAsync(id, cancellationToken);
        mail.SentAt = now;
        mail.LastError = null;
        ClearBody(mail);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(long id, string error, DateTimeOffset? nextAttemptAt, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var mail = await FindTrackedAsync(id, cancellationToken);
        mail.LastError = error.Length > MailOutboxEntityConfiguration.LastErrorMaxLength
            ? error[..MailOutboxEntityConfiguration.LastErrorMaxLength]
            : error;
        if (nextAttemptAt is { } next)
        {
            mail.NextAttemptAt = next;
        }
        else
        {
            mail.FailedAt = now;
            ClearBody(mail);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<MailOutboxEntity> FindTrackedAsync(long id, CancellationToken cancellationToken) =>
        await dbContext.MailOutbox.AsTracking().SingleAsync(mail => mail.Id == id, cancellationToken);

    private static void ClearBody(MailOutboxEntity mail)
    {
        mail.TextBody = null;
        mail.HtmlBody = null;
    }
}
