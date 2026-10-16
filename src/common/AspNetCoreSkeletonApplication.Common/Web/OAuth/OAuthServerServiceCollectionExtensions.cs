using System.Security.Cryptography.X509Certificates;
using AspNetCoreSkeletonApplication.Common.Web.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AspNetCoreSkeletonApplication.Common.Web.OAuth;

public static class OAuthServerServiceCollectionExtensions
{
    public const string SectionName = "Authentication:OAuthServer";

    /// <summary>
    /// OAuth 2.0 인가 서버 + OpenID Connect(OpenIddict). 엔드포인트는 <c>/connect/authorize</c> · <c>token</c> · <c>userinfo</c> · <c>endsession</c>,
    /// 탐색 문서 <c>/.well-known/openid-configuration</c>. 흐름은 인가 코드 + PKCE(필수) · 리프레시 토큰(회전) · 클라이언트 자격 증명뿐이다.
    /// issuer 는 <c>App:PublicOrigin</c> — 사용자가 여는 출처와 같다. 요청 처리는 <see cref="OAuthServerEndpoints"/> 가 맡는다(passthrough).
    /// </summary>
    public static IServiceCollection AddSkeletonOAuthServer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OAuthServerOptions>().BindConfiguration(SectionName).ValidateDataAnnotations()
            .Validate(Validate, "Authentication:OAuthServer 구성이 맞지 않는다 — KeySource 가 Certificates 면 두 인증서가, 클라이언트 자격 증명을 쓰는 클라이언트는 ClientSecret 이 필요하고, AllowedOrigins 는 경로 없는 http(s) 출처여야 한다")
            .ValidateOnStart();
        var server = configuration.GetSection(SectionName).Get<OAuthServerOptions>() ?? new OAuthServerOptions();
        var issuer = configuration["App:PublicOrigin"];
        services.AddCors(cors => cors.AddPolicy(OAuthServerEndpoints.CorsPolicy, policy => policy
            .WithOrigins(server.AllowedOrigins.Where(IsOrigin).Select(origin => new Uri(origin).GetLeftPart(UriPartial.Authority)).ToArray())
            .WithMethods(HttpMethods.Get, HttpMethods.Post)
            .AllowAnyHeader()));

        services.AddOpenIddict().AddServer(options =>
        {
            if (Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri))
            {
                options.SetIssuer(issuerUri);
            }

            options.SetAuthorizationEndpointUris("connect/authorize")
                .SetTokenEndpointUris("connect/token")
                .SetUserInfoEndpointUris("connect/userinfo")
                .SetEndSessionEndpointUris("connect/endsession");
            options.AllowAuthorizationCodeFlow().AllowRefreshTokenFlow().AllowClientCredentialsFlow();
            options.RequireProofKeyForCodeExchange();
            options.RegisterScopes(OAuthScopes.All.ToArray());
            // 탐색 문서의 prompt_values_supported — 실제로 처리하는 값만 둔다(OAuthServerEndpoints 의 authorize). 계정을 여럿 두지 않으므로
            // select_account 는 받지 않는다(받으면 OpenIddict 가 invalid_request 로 거절한다).
            options.Configure(server =>
            {
                server.PromptValues.Clear();
                server.PromptValues.UnionWith([PromptValues.None, PromptValues.Login, PromptValues.Consent]);
            });

            if (server.KeySource == OAuthKeySource.Certificates)
            {
                if (server.SigningCertificates.Count == 0 || server.EncryptionCertificates.Count == 0)
                {
                    throw new OptionsValidationException(SectionName, typeof(OAuthServerOptions),
                        ["KeySource 가 Certificates 면 SigningCertificates · EncryptionCertificates 가 하나 이상 필요하다"]);
                }

                // OpenIddict 가 지금 유효한 것 중 만료가 가장 늦은 인증서를 고른다 — 넣는 순서는 상관없다.
                foreach (var certificate in server.SigningCertificates)
                {
                    options.AddSigningCertificate(Load(certificate));
                }

                foreach (var certificate in server.EncryptionCertificates)
                {
                    options.AddEncryptionCertificate(Load(certificate));
                }
            }
            else
            {
                options.AddEphemeralSigningKey().AddEphemeralEncryptionKey();
            }

            options.UseAspNetCore()
                .EnableAuthorizationEndpointPassthrough()
                .EnableTokenEndpointPassthrough()
                .EnableUserInfoEndpointPassthrough()
                .EnableEndSessionEndpointPassthrough();
        });

        // 이 서버가 발급한 토큰을 API 가 받는다 — Bearer 헤더가 있으면 토큰으로, 없으면 세션 쿠키로 인증한다(정책 스킴 하나가 고른다).
        services.AddOpenIddict().AddValidation(options =>
        {
            options.UseLocalServer();
            options.UseAspNetCore();
        });
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = BearerAuthentication.SessionOrBearerScheme;
                options.DefaultChallengeScheme = BearerAuthentication.SessionOrBearerScheme;
                options.DefaultForbidScheme = BearerAuthentication.SessionOrBearerScheme;
            })
            .AddPolicyScheme(BearerAuthentication.SessionOrBearerScheme, "Session cookie or bearer token", options =>
                options.ForwardDefaultSelector = context => BearerAuthentication.IsBearer(context.Request)
                    ? OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme
                    : IdentityConstants.ApplicationScheme);

        services.AddHostedService<OAuthClientSynchronizer>();
        // 만료 토큰 정리 — 기본은 Batch 가 한다. RunInServer 일 때만 Server 가 한다(Batch 없이 배포할 때).
        services.AddOptions<OAuthTokenPruningOptions>().BindConfiguration(SectionName + ":TokenPruning").ValidateDataAnnotations().ValidateOnStart();
        services.AddHostedService<OAuthTokenPruneWorker>();
        return services;
    }

    private static bool Validate(OAuthServerOptions options) =>
        (options.KeySource != OAuthKeySource.Certificates
         || (options.SigningCertificates.Count > 0 && options.EncryptionCertificates.Count > 0
             && options.SigningCertificates.Concat(options.EncryptionCertificates).All(certificate => !string.IsNullOrWhiteSpace(certificate))))
        && options.Clients.All(client =>
            !client.GrantTypes.Contains(GrantTypes.ClientCredentials) || !string.IsNullOrWhiteSpace(client.ClientSecret))
        && options.AllowedOrigins.All(IsOrigin);

    /// <summary>경로 · 쿼리 없는 http(s) 출처인가 — <c>https://app.example.com</c>.</summary>
    internal static bool IsOrigin(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);

    private static X509Certificate2 Load(string base64) =>
        X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(base64), password: null);
}
