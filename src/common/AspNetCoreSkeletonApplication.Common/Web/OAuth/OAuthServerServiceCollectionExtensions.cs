using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
            .Validate(Validate, "Authentication:OAuthServer 구성이 맞지 않는다 — KeySource 가 Certificates 면 두 인증서가, 클라이언트 자격 증명을 쓰는 클라이언트는 ClientSecret 이 필요하다")
            .ValidateOnStart();
        var server = configuration.GetSection(SectionName).Get<OAuthServerOptions>() ?? new OAuthServerOptions();
        var issuer = configuration["App:PublicOrigin"];

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

            if (server.KeySource == OAuthKeySource.Certificates)
            {
                options.AddSigningCertificate(Load(server.SigningCertificate))
                    .AddEncryptionCertificate(Load(server.EncryptionCertificate));
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

        services.AddHostedService<OAuthClientSynchronizer>();
        return services;
    }

    private static bool Validate(OAuthServerOptions options) =>
        (options.KeySource != OAuthKeySource.Certificates
         || (!string.IsNullOrWhiteSpace(options.SigningCertificate) && !string.IsNullOrWhiteSpace(options.EncryptionCertificate)))
        && options.Clients.All(client =>
            !client.GrantTypes.Contains(GrantTypes.ClientCredentials) || !string.IsNullOrWhiteSpace(client.ClientSecret));

    private static X509Certificate2 Load(string? base64) =>
        X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(base64 ?? throw new OptionsValidationException(
            SectionName, typeof(OAuthServerOptions), ["KeySource 가 Certificates 면 SigningCertificate · EncryptionCertificate 가 필요하다"])), password: null);
}
