namespace AspNetCoreSkeletonApplication.Common.Mail.Dtos;

/// <summary>발송하려고 잡은 아웃박스 메일. <see cref="Attempts"/> 는 이번 시도를 센 값이다.</summary>
public sealed record MailOutboxDto(long Id, string Recipient, string Subject, string TextBody, string HtmlBody, int Attempts);
