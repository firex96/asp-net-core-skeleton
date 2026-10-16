using AspNetCoreSkeletonApplication.Common.Mail.Clients;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.Common.Mail;

public static class MailServiceCollectionExtensions
{
    /// <summary>
    /// 메일 — Identity 계정 메일은 아웃박스에 넣고, 백그라운드(<see cref="MailOutboxWorker"/>)가 SMTP 로 보낸다.
    /// 구성 <c>Mail:Smtp</c> 는 기동할 때 검사한다(빠지면 멈춘다). <c>MailOutboxBizService</c> 는 <c>AddCommon</c> 이 등록한다.
    /// </summary>
    public static IServiceCollection AddSkeletonMail(this IServiceCollection services)
    {
        services.AddOptions<SmtpOptions>().BindConfiguration("Mail:Smtp").ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<MailOutboxOptions>().BindConfiguration("Mail:Outbox");
        services.AddSingleton<IMailClient, SmtpMailClient>();
        services.AddScoped<IEmailSender<ApplicationUser>, IdentityEmailSender>();
        services.AddHostedService<MailOutboxWorker>();
        return services;
    }
}
