using AspNetCoreSkeletonApplication.Common.Persistence.Entities.Backbone;

namespace AspNetCoreSkeletonApplication.Common.Mail.Persistence.Entities;

/// <summary>
/// 보낼 메일 한 통. 요청은 이 행만 쓰고 끝나고, 발송은 백그라운드(<c>MailOutboxWorker</c>)가 한다. 보냈거나 포기하면 본문을 비운다 —
/// 재설정 링크 같은 토큰을 DB 에 남기지 않는다. 행은 지우지 않는다(발송 기록).
/// </summary>
internal sealed class MailOutboxEntity : BaseAuditedEntity
{
    public string Recipient { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string? TextBody { get; set; }

    public string? HtmlBody { get; set; }

    /// <summary>지금까지 시도한 횟수 — 발송하려고 잡을 때 센다.</summary>
    public int Attempts { get; set; }

    /// <summary>이 시각부터 발송 대상이다. 발송 중에는 잡아 둔 기한(다른 인스턴스가 다시 잡지 않는다)이다.</summary>
    public DateTimeOffset NextAttemptAt { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    /// <summary>최대 시도 횟수를 넘겨 포기한 시각.</summary>
    public DateTimeOffset? FailedAt { get; set; }

    public string? LastError { get; set; }
}
