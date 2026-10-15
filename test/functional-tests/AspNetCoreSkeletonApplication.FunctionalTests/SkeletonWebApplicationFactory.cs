using AspNetCoreSkeletonApplication.FunctionalTests.Support;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

/// <summary>
/// 비밀값 출처 없이(Testing 환경) 앱을 띄운다. DB · Redis 에는 붙지 않으므로 연결 문자열은 형식만 맞춘 값이다.
/// 인증은 <see cref="TestAuthenticationHandler"/>(<c>X-Test-User</c> 헤더)로 바꾼다 — 인가 규칙은 앱과 같다.
/// </summary>
public class SkeletonWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "Server=unused;Database=unused");
        builder.UseSetting("ConnectionStrings:Redis", "unused:6379");
        builder.UseSetting("OpenApi:Enabled", "true");
        builder.ConfigureTestServices(services =>
        {
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
