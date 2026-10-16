using AspNetCoreSkeletonApplication.Common.Mail.Dtos;

namespace AspNetCoreSkeletonApplication.Common.Mail.Clients;

/// <summary>메일 한 통을 실제로 보낸다. 실패하면 예외를 던진다 — 보냈다고 가정하고 넘어가지 않는다. 부르는 곳은 아웃박스 발송(<c>MailOutboxBizService</c>)뿐이다.</summary>
public interface IMailClient
{
    Task SendAsync(OutgoingMailDto mail, CancellationToken cancellationToken);
}
