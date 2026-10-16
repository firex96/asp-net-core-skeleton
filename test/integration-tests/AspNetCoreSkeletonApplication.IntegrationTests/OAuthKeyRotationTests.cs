using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>
/// OAuth 서버 키 교체 — 지금 유효한 서명 인증서와 아직 유효 기간 전인 새 인증서를 함께 넣으면, 공개 키(JWKS)에는 둘 다 실리고
/// 서명은 지금 유효한 것으로 한다(교체 ① 단계).
/// </summary>
[Collection(InfrastructureCollection.Name)]
public class OAuthKeyRotationTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string Database = "oauth_key_rotation";

    private readonly TestCertificate _current = Certificate(X509KeyUsageFlags.DigitalSignature, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    private readonly TestCertificate _next = Certificate(X509KeyUsageFlags.DigitalSignature, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(60));
    private readonly TestCertificate _encryption = Certificate(X509KeyUsageFlags.KeyEncipherment, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    private WebApplicationFactory<Program> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await infrastructure.RecreateDatabaseAsync(Database);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            infrastructure.ConfigureApp(builder, Database);
            builder.UseSetting("Authentication:OAuthServer:KeySource", "Certificates");
            // 새 인증서를 앞 칸에 넣는다 — 순서가 아니라 유효 기간으로 고르는지 본다.
            builder.UseSetting("Authentication:OAuthServer:SigningCertificates:0", _next.Pfx);
            builder.UseSetting("Authentication:OAuthServer:SigningCertificates:1", _current.Pfx);
            builder.UseSetting("Authentication:OAuthServer:EncryptionCertificates:0", _encryption.Pfx);
            OAuthFlows.RegisterClient(builder, 0, "first-party", consent: "Implicit", grants: ["authorization_code"], scopes: ["openid"]);
        });
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task BothSigningKeysArePublished_AndTheCurrentOneSigns()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = TestUsers.Https.BaseAddress, AllowAutoRedirect = false });
        var keys = (await client.GetFromJsonAsync<JsonElement>("/.well-known/jwks", Token)).GetProperty("keys").EnumerateArray().ToArray();

        var thumbprints = keys.Select(key => key.GetProperty("x5t").GetString()).ToArray();
        Assert.Contains(_current.Thumbprint, thumbprints);
        Assert.Contains(_next.Thumbprint, thumbprints);

        var (browser, _) = await TestUsers.SignInAsync(_factory, "rotation@example.com", administrator: false);
        browser.Dispose();
        var signedIn = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = TestUsers.Https.BaseAddress, AllowAutoRedirect = false });
        (await signedIn.PostAsJsonAsync("/api/auth/login", new { email = "rotation@example.com", password = TestUsers.Password }, Token)).EnsureSuccessStatusCode();
        var pkce = OAuthFlows.Pkce();
        var code = await OAuthFlows.AuthorizeCodeAsync(signedIn, OAuthFlows.AuthorizeUrl("first-party", "openid", pkce.Challenge));
        var tokens = await OAuthFlows.TokenAsync(client, new()
        {
            ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = OAuthFlows.Callback,
            ["client_id"] = "first-party", ["code_verifier"] = pkce.Verifier,
        });

        var header = JsonDocument.Parse(WebEncoders.Base64UrlDecode(tokens.GetProperty("id_token").GetString()!.Split('.')[0])).RootElement;
        var signingKey = keys.Single(key => key.GetProperty("kid").GetString() == header.GetProperty("kid").GetString());
        Assert.Equal(_current.Thumbprint, signingKey.GetProperty("x5t").GetString());
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>구성에 넣을 PKCS#12 base64 와 JWKS 의 <c>x5t</c>(SHA-1 지문, base64url). 만들 때 바로 뽑는다 — 다시 읽은 인증서는 플랫폼에 따라 키를 내보내지 못한다.</summary>
    private sealed record TestCertificate(string Pfx, string Thumbprint);

    private static TestCertificate Certificate(X509KeyUsageFlags usage, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=oauth-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(usage, critical: true));
        using var certificate = request.CreateSelfSigned(notBefore, notAfter);
        return new TestCertificate(Convert.ToBase64String(certificate.Export(X509ContentType.Pfx)), WebEncoders.Base64UrlEncode(certificate.GetCertHash()));
    }
}
