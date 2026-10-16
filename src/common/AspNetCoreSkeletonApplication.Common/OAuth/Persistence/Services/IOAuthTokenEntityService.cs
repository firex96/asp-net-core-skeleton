namespace AspNetCoreSkeletonApplication.Common.OAuth.Persistence.Services;

/// <summary>OAuth 서버 저장소(<c>oauth_token</c> · <c>oauth_authorization</c>)의 정리. 저장소는 OpenIddict 가 소유하므로 그 관리자를 거친다.</summary>
public interface IOAuthTokenEntityService
{
    /// <summary><paramref name="threshold"/> 전에 만들어진 토큰 중 더 쓸 수 없는 것(만료 · 폐기 · 사용됨)을 지운다. 지운 수를 돌려준다.</summary>
    Task<long> PruneTokensAsync(DateTimeOffset threshold, CancellationToken cancellationToken);

    /// <summary><paramref name="threshold"/> 전에 만들어진 동의 중 더 쓸 수 없는 것(폐기됨 · 토큰 없는 일회성)을 지운다.</summary>
    Task<long> PruneAuthorizationsAsync(DateTimeOffset threshold, CancellationToken cancellationToken);
}
