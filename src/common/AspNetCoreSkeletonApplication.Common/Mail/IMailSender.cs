namespace AspNetCoreSkeletonApplication.Common.Mail;

/// <summary>메일 발송. 실패하면 예외를 던진다 — 보냈다고 가정하고 넘어가지 않는다.</summary>
public interface IMailSender
{
    Task SendAsync(OutgoingMail mail, CancellationToken cancellationToken);
}
