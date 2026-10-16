using System.ComponentModel.DataAnnotations;
using MailKit.Security;

namespace AspNetCoreSkeletonApplication.Common.Mail.Clients;

/// <summary>
/// 구성 경로 <c>Mail:Smtp</c>. 모두 필수다 — 기동할 때 검사해 빠진 키 이름을 담아 멈춘다. <c>Password</c> 는 비밀값이다.
/// 인증 없는 SMTP 는 두지 않는다(로컬 Mailpit 도 아무 계정이나 받도록 띄운다) — 인증 여부로 갈리는 분기가 없다.
/// </summary>
public sealed class SmtpOptions
{
    [Required]
    public string Host { get; set; } = null!;

    [Range(1, 65535)]
    public int Port { get; set; }

    /// <summary><c>StartTls</c>(587) · <c>SslOnConnect</c>(465) · <c>None</c>(로컬 Mailpit 만).</summary>
    [Required]
    public SecureSocketOptions? Security { get; set; }

    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    /// <summary>보내는 주소. <c>"이름 &lt;주소&gt;"</c> 형식도 된다.</summary>
    [Required]
    public string From { get; set; } = null!;
}
