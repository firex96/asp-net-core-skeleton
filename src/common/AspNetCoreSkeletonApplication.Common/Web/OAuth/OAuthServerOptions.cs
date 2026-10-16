using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AspNetCoreSkeletonApplication.Common.Web.OAuth;

/// <summary>구성 경로 <c>Authentication:OAuthServer</c>. 기동할 때 검사한다.</summary>
public sealed class OAuthServerOptions
{
    /// <summary>
    /// 토큰 서명 · 암호화 키를 어디서 얻나. <c>Certificates</c> — <see cref="SigningCertificate"/> · <see cref="EncryptionCertificate"/>(비밀값),
    /// 인스턴스가 여럿이어도 같은 키다. <c>Ephemeral</c> — 기동할 때마다 새 키(로컬 · 테스트 전용: 재기동하면 발급한 토큰이 모두 무효, 인스턴스끼리 서로의 토큰을 못 읽는다).
    /// 기본값이 없다 — 배포가 고른다.
    /// </summary>
    [Required]
    public OAuthKeySource? KeySource { get; set; }

    /// <summary>서명용 X.509 인증서(PKCS#12, 암호 없음)의 base64. 키 사용 <c>DigitalSignature</c>.</summary>
    public string? SigningCertificate { get; set; }

    /// <summary>암호화용 X.509 인증서(PKCS#12, 암호 없음)의 base64. 키 사용 <c>KeyEncipherment</c>.</summary>
    public string? EncryptionCertificate { get; set; }

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
