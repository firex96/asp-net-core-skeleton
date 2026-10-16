using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.Common.Mail;

public static class MailServiceCollectionExtensions
{
    /// <summary>SMTP 메일 발송과 Identity 계정 메일. 구성 <c>Mail:Smtp</c> 는 기동할 때 검사한다(빠지면 멈춘다).</summary>
    public static IServiceCollection AddSkeletonMail(this IServiceCollection services)
    {
        services.AddOptions<SmtpOptions>().BindConfiguration("Mail:Smtp").ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton<IMailSender, SmtpMailSender>();
        services.AddSingleton<IEmailSender<ApplicationUser>, IdentityEmailSender>();
        return services;
    }
}
