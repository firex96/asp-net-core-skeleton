using OpenIddict.Abstractions;

namespace AspNetCoreSkeletonApplication.Common.OAuth.Persistence.Services;

/// <summary>무엇을 지울지는 OpenIddict 의 정리 규칙을 그대로 쓴다 — 상태 · 만료 판단을 다시 구현하지 않는다.</summary>
internal sealed class OAuthTokenEntityService(
    IOpenIddictTokenManager tokenManager, IOpenIddictAuthorizationManager authorizationManager) : IOAuthTokenEntityService
{
    public async Task<long> PruneTokensAsync(DateTimeOffset threshold, CancellationToken cancellationToken) =>
        await tokenManager.PruneAsync(threshold, cancellationToken);

    public async Task<long> PruneAuthorizationsAsync(DateTimeOffset threshold, CancellationToken cancellationToken) =>
        await authorizationManager.PruneAsync(threshold, cancellationToken);
}
