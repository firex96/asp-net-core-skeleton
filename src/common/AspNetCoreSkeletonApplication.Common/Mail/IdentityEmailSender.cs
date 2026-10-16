using System.Net;
using AspNetCoreSkeletonApplication.Common.Mail.Dtos;
using AspNetCoreSkeletonApplication.Common.Mail.Services.Biz;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Microsoft.AspNetCore.Identity;

namespace AspNetCoreSkeletonApplication.Common.Mail;

/// <summary>
/// Identity 가 보내는 계정 메일 — 아웃박스에 넣고 돌아온다(발송은 백그라운드). 링크는 호출자가 <c>App:PublicOrigin</c> 으로 만든다 — 요청의 Host 헤더로 만들지 않는다.
/// </summary>
internal sealed class IdentityEmailSender(MailOutboxBizService outbox) : IEmailSender<ApplicationUser>
{
    private const string Product = "AspNetCoreSkeletonApplication";

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var encoded = WebUtility.HtmlEncode(resetLink);
        return outbox.EnqueueAsync(new OutgoingMailDto(
            email,
            $"[{Product}] 비밀번호 재설정 안내",
            $"비밀번호를 다시 정하려면 아래 링크를 열어 주세요. 링크는 2시간 동안 한 번만 쓸 수 있어요.\n\n{resetLink}\n\n비밀번호 재설정을 요청한 적이 없다면 이 메일은 무시해도 괜찮아요.",
            $"<p>비밀번호를 다시 정하려면 아래 링크를 눌러 주세요. 링크는 2시간 동안 한 번만 쓸 수 있어요.</p><p><a href=\"{encoded}\">비밀번호 재설정하기</a></p><p>비밀번호 재설정을 요청한 적이 없다면 이 메일은 무시해도 괜찮아요.</p>"),
            CancellationToken.None);
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var encoded = WebUtility.HtmlEncode(confirmationLink);
        return outbox.EnqueueAsync(new OutgoingMailDto(
            email,
            $"[{Product}] 이메일 확인 안내",
            $"가입해 주셔서 고마워요. 아래 링크를 열어 이메일을 확인하면 로그인할 수 있어요. 링크는 2시간 동안 쓸 수 있어요.\n\n{confirmationLink}\n\n가입한 적이 없다면 이 메일은 무시해도 괜찮아요.",
            $"<p>가입해 주셔서 고마워요. 아래 링크를 눌러 이메일을 확인하면 로그인할 수 있어요. 링크는 2시간 동안 쓸 수 있어요.</p><p><a href=\"{encoded}\">이메일 확인하기</a></p><p>가입한 적이 없다면 이 메일은 무시해도 괜찮아요.</p>"),
            CancellationToken.None);
    }

    /// <summary>2단계 인증의 로그인 확인 코드 — Identity 의 <see cref="IEmailSender{TUser}"/> 에는 없는 메일이라 따로 둔다.</summary>
    public Task SendSignInCodeAsync(string email, string code) =>
        outbox.EnqueueAsync(new OutgoingMailDto(
            email,
            $"[{Product}] 로그인 확인 코드",
            $"로그인을 마치려면 아래 확인 코드를 입력해 주세요. 코드는 몇 분 동안만 쓸 수 있어요.\n\n{code}\n\n로그인한 적이 없다면 비밀번호를 바꿔 주세요.",
            $"<p>로그인을 마치려면 아래 확인 코드를 입력해 주세요. 코드는 몇 분 동안만 쓸 수 있어요.</p><p style=\"font-size:24px;letter-spacing:4px\"><strong>{code}</strong></p><p>로그인한 적이 없다면 비밀번호를 바꿔 주세요.</p>"),
            CancellationToken.None);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        throw new NotSupportedException("재설정은 링크로만 보낸다");
}
