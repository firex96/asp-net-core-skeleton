using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AspNetCoreSkeletonApplication.Common.Web.OAuth;

/// <summary>구성 경로 <c>Authentication:OAuthServer</c>. 기동할 때 검사한다.</summary>
public sealed class OAuthServerOptions
{
    /// <summary>
    /// 토큰 서명 · 암호화 키를 어디서 얻나. <c>Certificates</c> — <see cref="SigningCertificates"/> · <see cref="EncryptionCertificates"/>(비밀값),
    /// 인스턴스가 여럿이어도 같은 키다. <c>Ephemeral</c> — 기동할 때마다 새 키(로컬 · 테스트 전용: 재기동하면 발급한 토큰이 모두 무효, 인스턴스끼리 서로의 토큰을 못 읽는다).
    /// 기본값이 없다 — 배포가 고른다.
    /// </summary>
    [Required]
    public OAuthKeySource? KeySource { get; set; }

    /// <summary>
    /// 서명용 X.509 인증서(PKCS#12, 암호 없음)의 base64 들. 키 사용 <c>DigitalSignature</c>. 둘 이상이면 교체 중이다 — 지금 유효한 것 중
    /// 만료가 가장 늦은 것으로 서명하고, 공개 키(JWKS)에는 모두 싣는다. 아직 유효 기간 전인 새 인증서를 미리 넣어 두면 그 시각 뒤 재기동할 때
    /// 넘어가고, 이전 인증서로 서명한 토큰도 검증된다(README 「OAuth 서버」 키 교체).
    /// </summary>
    public List<string> SigningCertificates { get; set; } = [];

    /// <summary>암호화용 X.509 인증서(PKCS#12, 암호 없음)의 base64 들. 키 사용 <c>KeyEncipherment</c>. 교체는 서명 인증서와 같다.</summary>
    public List<string> EncryptionCertificates { get; set; } = [];

    /// <summary>
    /// OAuth 엔드포인트를 브라우저에서 직접 부르는 다른 출처의 앱(다른 도메인의 SPA 등). <c>/connect/*</c> · <c>/.well-known/*</c> 에만 CORS 를 연다 —
    /// 자격 증명(쿠키)은 싣지 않는다. 비우면 열지 않는다. 출처는 <c>https://app.example.com</c> 처럼 경로 없이 쓴다.
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = [];

    /// <summary>등록할 클라이언트. 기동할 때 저장소와 맞춘다(만들거나 고친다 — 목록에서 빠진 클라이언트는 지우지 않는다).</summary>
    public List<OAuthClientOptions> Clients { get; set; } = [];
}

public enum OAuthKeySource
{
    [JsonStringEnumMemberName("Certificates")]
    Certificates,

    [JsonStringEnumMemberName("Ephemeral")]
    Ephemeral,
}

/// <summary>클라이언트 하나. <c>ClientSecret</c> 이 있으면 기밀 클라이언트(비밀값), 없으면 공개 클라이언트(인가 코드 + PKCE 만).</summary>
public sealed class OAuthClientOptions
{
    [Required]
    public string ClientId { get; set; } = null!;

    public string? ClientSecret { get; set; }

    [Required]
    public string DisplayName { get; set; } = null!;

    /// <summary><c>Explicit</c> — 사용자가 동의 화면에서 허락해야 한다(한 번 허락하면 저장). <c>Implicit</c> — 자사 클라이언트, 묻지 않는다.</summary>
    [Required]
    public OAuthConsentType? ConsentType { get; set; }

    /// <summary><c>authorization_code</c> · <c>refresh_token</c> · <c>client_credentials</c>.</summary>
    public List<string> GrantTypes { get; set; } = [];

    public List<string> RedirectUris { get; set; } = [];

    public List<string> PostLogoutRedirectUris { get; set; } = [];

    /// <summary>요청할 수 있는 scope — <c>openid</c> · <c>profile</c> · <c>email</c> · <c>offline_access</c> 와 권한 코드.</summary>
    public List<string> Scopes { get; set; } = [];
}

public enum OAuthConsentType
{
    [JsonStringEnumMemberName("Explicit")]
    Explicit,

    [JsonStringEnumMemberName("Implicit")]
    Implicit,
}
