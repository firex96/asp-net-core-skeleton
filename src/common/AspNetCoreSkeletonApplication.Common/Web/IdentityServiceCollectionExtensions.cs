using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
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
    /// 로그인 — ASP.NET Core Identity(계정 · 비밀번호 해시 · 잠금) + 쿠키, 그리고 권한 인가. 같은 출처 SPA 가 호출하는 앱(Server)이 부른다.
    /// <list type="bullet">
    /// <item>쿠키는 <c>__Host-</c> 이름, HttpOnly · Secure · SameSite=Strict. 로그인 페이지가 없으므로 인증 필요는 401, 권한 없음은 403 이다.</item>
    /// <item>계정 잠금 — 5번 틀리면 30분(형제 스켈레톤과 같은 값).</item>
    /// <item>이메일을 확인해야 로그인한다(<c>RequireConfirmedEmail</c>) — 비밀번호 · 소셜 로그인 모두.</item>
    /// <item>기본 정책은 로그인 필요(fallback). 권한은 <see cref="HasPermissionAttribute"/> 로 선언하고, 요청마다 그룹 · 역할 · 권한으로 판정한다
    /// (<see cref="PermissionAuthorizationHandler"/>). 역할을 Identity 에 두지 않는다 — 인가 도메인이 맡는다.</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddSkeletonIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequiredLength = 10;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddUserStore<ApplicationUserStore>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<KoreanIdentityErrorDescriber>();

        // 비밀번호 재설정 · 이메일 확인 토큰은 2시간. 재설정이 보안 스탬프를 바꾸므로 재설정 토큰은 한 번만 쓰이고, 다른 기기의 세션도 1분 안에 끊긴다.
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(2));
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(1));
        services.AddOptions<AppOptions>().BindConfiguration("App").ValidateDataAnnotations().ValidateOnStart();

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
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.AddHttpContextAccessor();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<RequestPermissions>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }

    // 본문은 UseStatusCodePages 가 ProblemDetails 로 채운다.
    private static Task Status(HttpResponse response, int statusCode)
    {
        response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}
