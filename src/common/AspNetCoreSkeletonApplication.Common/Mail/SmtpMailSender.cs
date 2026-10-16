using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AspNetCoreSkeletonApplication.Common.Mail;

/// <summary>MailKit 으로 SMTP 발송. 연결은 메일마다 열고 닫는다(발송량이 적은 계정 메일 기준). 로그에는 가린 받는 주소와 제목만 남긴다.</summary>
internal sealed partial class SmtpMailSender(IOptions<SmtpOptions> options, ILogger<SmtpMailSender> logger) : IMailSender
{
    public async Task SendAsync(OutgoingMail mail, CancellationToken cancellationToken)
    {
        var smtp = options.Value;
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(smtp.From));
        message.To.Add(MailboxAddress.Parse(mail.To));
        message.Subject = mail.Subject;
        message.Body = new BodyBuilder { TextBody = mail.TextBody, HtmlBody = mail.HtmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, smtp.Security!.Value, cancellationToken);
        await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
        LogSent(Mask(mail.To), mail.Subject);
    }

    /// <summary><c>abcdef@example.com</c> → <c>ab***@example.com</c>.</summary>
    internal static string Mask(string address)
    {
        var at = address.IndexOf('@', StringComparison.Ordinal);
        return at <= 0 ? "***" : $"{address[..Math.Min(2, at)]}***{address[at..]}";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "메일을 보냈다 — 받는 사람 {To}, 제목 {Subject}")]
    private partial void LogSent(string to, string subject);
}
