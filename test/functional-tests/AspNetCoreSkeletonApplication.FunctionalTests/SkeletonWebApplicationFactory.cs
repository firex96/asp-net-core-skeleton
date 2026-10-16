using AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;
using AspNetCoreSkeletonApplication.FunctionalTests.Support;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

/// <summary>
/// 비밀값 출처 없이(Testing 환경) 앱을 띄운다. DB · Redis · SMTP 에는 붙지 않으므로 연결 값은 형식만 맞춘 값이다.
/// 인증은 <see cref="TestAuthenticationHandler"/>(<c>X-Test-User</c> 헤더)로, 권한 판정은 <see cref="ClaimsPermissionEvaluator"/> 로 바꾼다 — 인가 규칙은 앱과 같다.
/// </summary>
public class SkeletonWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "Server=unused;Database=unused");
        builder.UseSetting("ConnectionStrings:Redis", "unused:6379");
        builder.UseSetting("OpenApi:Enabled", "true");
        builder.UseSetting("App:PublicOrigin", "https://spa.test");
        builder.UseSetting("Mail:Smtp:Host", "unused");
        builder.UseSetting("Mail:Smtp:Port", "25");
        builder.UseSetting("Mail:Smtp:Security", "None");
        builder.UseSetting("Mail:Smtp:Username", "unused");
        builder.UseSetting("Mail:Smtp:Password", "unused");
        builder.UseSetting("Mail:Smtp:From", "no-reply@spa.test");
        builder.UseSetting("Authentication:OAuthServer:KeySource", "Ephemeral");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPermissionEvaluator>();
            services.AddScoped<IPermissionEvaluator, ClaimsPermissionEvaluator>();
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, null);
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
            });
        });
    }
}
