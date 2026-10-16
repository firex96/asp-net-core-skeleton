namespace AspNetCoreSkeletonApplication.Common.Mail.Dtos;

/// <summary>보낼 메일 한 통. 본문은 로그에 남기지 않는다 — 비밀번호 재설정 링크 같은 토큰이 들어 있다.</summary>
public sealed record OutgoingMailDto(string To, string Subject, string TextBody, string HtmlBody);
