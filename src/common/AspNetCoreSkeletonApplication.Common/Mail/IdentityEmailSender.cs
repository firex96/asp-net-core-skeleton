using System.Net;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Microsoft.AspNetCore.Identity;

namespace AspNetCoreSkeletonApplication.Common.Mail;

/// <summary>Identity 가 보내는 계정 메일. 링크는 호출자가 <c>App:PublicOrigin</c> 으로 만든다 — 요청의 Host 헤더로 만들지 않는다.</summary>
internal sealed class IdentityEmailSender(IMailSender mailSender) : IEmailSender<ApplicationUser>
{
    private const string Product = "AspNetCoreSkeletonApplication";

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var encoded = WebUtility.HtmlEncode(resetLink);
        return mailSender.SendAsync(new OutgoingMail(
            email,
            $"[{Product}] 비밀번호 재설정",
            $"아래 링크에서 새 비밀번호를 정하세요. 링크는 2시간 동안 한 번만 쓸 수 있습니다.\n\n{resetLink}\n\n요청하지 않았다면 이 메일을 무시하세요.",
            $"<p>아래 링크에서 새 비밀번호를 정하세요. 링크는 2시간 동안 한 번만 쓸 수 있습니다.</p><p><a href=\"{encoded}\">비밀번호 재설정</a></p><p>요청하지 않았다면 이 메일을 무시하세요.</p>"),
            CancellationToken.None);
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        throw new NotSupportedException("이메일 확인 흐름은 아직 없다 — README 「후속 과제」");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        throw new NotSupportedException("재설정은 링크로만 보낸다");
}
