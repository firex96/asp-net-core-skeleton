namespace AspNetCoreSkeletonApplication.Common.Mail;

/// <summary>구성 경로 <c>Mail:Outbox</c> — 아웃박스 발송 주기와 한 번에 잡는 양.</summary>
public sealed class MailOutboxOptions
{
    /// <summary>보낼 메일이 없을 때 다시 볼 때까지 기다리는 시간.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>한 번에 잡는 메일 수. 다 찼으면 기다리지 않고 바로 다음 묶음을 잡는다.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>잡은 메일을 다른 인스턴스가 다시 잡지 못하는 시간 — 그 안에 보내지 못하고 멈추면(프로세스 종료) 이 시간 뒤에 다시 보낸다.</summary>
    public TimeSpan Lease { get; set; } = TimeSpan.FromMinutes(5);
}
