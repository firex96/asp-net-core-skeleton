using AspNetCoreSkeletonApplication.Common.Constants;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.Common.Web;

public static class IdentityServiceCollectionExtensions
{
    public const string CookieName = "__Host-session";

    /// <summary>
    /// 로그인 — ASP.NET Core Identity(계정 · 비밀번호 해시 · 잠금 · 역할) + 쿠키. 같은 출처 SPA 가 호출하는 앱(Server)이 부른다.
    /// <list type="bullet">
    /// <item>쿠키는 <c>__Host-</c> 이름, HttpOnly · Secure · SameSite=Strict. 로그인 페이지가 없으므로 인증 필요는 401, 권한 없음은 403 이다.</item>
    /// <item>계정 잠금 — 5번 틀리면 30분(형제 스켈레톤과 같은 값).</item>
    /// <item>기본 정책은 로그인 필요(fallback). 정책 <see cref="Policies.Administrator"/> — 역할 <see cref="Roles.Administrator"/>.</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddSkeletonIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager();

        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context => Status(context.Response, StatusCodes.Status401Unauthorized),
                OnRedirectToAccessDenied = context => Status(context.Response, StatusCodes.Status403Forbidden),
            };
        });

        // 기본은 로그인 필요다 — 익명으로 열 엔드포인트는 [AllowAnonymous] · AllowAnonymous() 로 하나씩 연다(열어 두는 쪽을 명시한다).
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.Administrator, policy => policy.RequireRole(Roles.Administrator));

        return services;
    }

    // 본문은 UseStatusCodePages 가 ProblemDetails 로 채운다.
    private static Task Status(HttpResponse response, int statusCode)
    {
        response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}
