using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.Common.Web;

public static class ExternalLoginServiceCollectionExtensions
{
    /// <summary>구성 섹션 이름 = 인증 스킴 이름 = SPA 가 보는 공급자 이름. 화면에 보이는 이름(카카오 · 네이버)은 등록할 때 따로 준다.</summary>
    public const string Google = "Google";
    public const string Kakao = "Kakao";
    public const string Naver = "Naver";

    /// <summary>소셜 로그인 공급자 이름 — 다른 인증 스킴(쿠키 · 토큰)과 섞이지 않게 이 목록으로 거른다.</summary>
    public static readonly IReadOnlyList<string> Providers = [Google, Kakao, Naver];

    /// <summary>
    /// 소셜 로그인 공급자. <c>Authentication:External:{Google|Kakao|Naver}</c> 섹션이 있는 공급자만 켠다 — 섹션이 있으면 <c>ClientId</c> ·
    /// <c>ClientSecret</c>(비밀값)는 필수다(없으면 기동하다 멈춘다). 섹션의 나머지 키는 핸들러 옵션에 그대로 묶인다 — 로컬은 엔드포인트
    /// 주소(<c>AuthorizationEndpoint</c> · <c>TokenEndpoint</c> · <c>UserInformationEndpoint</c>)를 mock 서버로 바꾼다.
    /// 외부 로그인은 Identity 의 외부 쿠키에 잠깐 담겼다가, 연결된 계정이면 세션 쿠키로 바뀐다(README 「인증」).
    /// </summary>
    public static IServiceCollection AddSkeletonExternalLogins(this IServiceCollection services, IConfiguration configuration)
    {
        var external = configuration.GetSection("Authentication:External");
        var authentication = services.AddAuthentication();
        if (Enabled(external, Google) is { } google)
        {
            authentication.AddGoogle(Google, Google, options => Configure(options, google, Google));
        }

        if (Enabled(external, Kakao) is { } kakao)
        {
            authentication.AddKakaoTalk(Kakao, "카카오", options =>
            {
                Configure(options, kakao, Kakao);
                options.Scope.Add("account_email");
            });
        }

        if (Enabled(external, Naver) is { } naver)
        {
            authentication.AddNaver(Naver, "네이버", options => Configure(options, naver, Naver));
        }

        return services;
    }

    private static IConfigurationSection? Enabled(IConfigurationSection external, string provider)
    {
        var section = external.GetSection(provider);
        if (!section.Exists())
        {
            return null;
        }

        foreach (var key in new[] { nameof(OAuthOptions.ClientId), nameof(OAuthOptions.ClientSecret) })
        {
            if (string.IsNullOrWhiteSpace(section[key]))
            {
                throw new InvalidOperationException($"구성 키 '{section.Path}:{key}' 가 없다 — 공급자를 켜려면 둘 다 있어야 한다");
            }
        }

        return section;
    }

    private static void Configure(OAuthOptions options, IConfigurationSection section, string provider)
    {
        section.Bind(options);
        options.SignInScheme = IdentityConstants.ExternalScheme;
        // SPA dev 서버가 /api 만 백엔드로 넘기므로 콜백도 /api 아래에 둔다.
        options.CallbackPath = $"/api/auth/external/signin-{provider.ToLowerInvariant()}";
        options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    }
}
