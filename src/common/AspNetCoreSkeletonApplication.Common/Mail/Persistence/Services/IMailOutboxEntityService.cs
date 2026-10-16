using AspNetCoreSkeletonApplication.Common.Mail.Dtos;

namespace AspNetCoreSkeletonApplication.Common.Mail.Persistence.Services;

public interface IMailOutboxEntityService
{
    /// <summary>보낼 메일을 넣는다. <paramref name="now"/> 부터 발송 대상이다.</summary>
    Task CreateAsync(OutgoingMailDto mail, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// 때가 된 메일을 최대 <paramref name="batchSize"/> 통 잡는다 — 시도 횟수를 하나 올리고 <paramref name="leaseUntil"/> 까지 다른 인스턴스가 잡지 못하게 한다.
    /// 다른 인스턴스가 잡고 있는 행은 건너뛴다.
    /// </summary>
    Task<IReadOnlyList<MailOutboxDto>> ClaimDueAsync(int batchSize, DateTimeOffset now, DateTimeOffset leaseUntil, CancellationToken cancellationToken);

    /// <summary>보냈다 — 본문을 비운다.</summary>
    Task MarkSentAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// 이번 시도가 실패했다. <paramref name="nextAttemptAt"/> 에 다시 시도하고, null 이면 포기한다(본문을 비운다).
    /// </summary>
    Task MarkFailedAsync(long id, string error, DateTimeOffset? nextAttemptAt, DateTimeOffset now, CancellationToken cancellationToken);
}
