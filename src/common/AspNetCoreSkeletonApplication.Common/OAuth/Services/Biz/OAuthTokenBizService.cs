using AspNetCoreSkeletonApplication.Common.OAuth.Domain;
using AspNetCoreSkeletonApplication.Common.OAuth.Dtos;
using AspNetCoreSkeletonApplication.Common.OAuth.Persistence.Services;
using AspNetCoreSkeletonApplication.Common.Persistence;

namespace AspNetCoreSkeletonApplication.Common.OAuth.Services.Biz;

/// <summary>OAuth 서버 저장소 정리(Batch 의 <c>PruneOAuthTokenJob</c>). 토큰을 먼저 지워야 토큰 없는 일회성 동의가 정리 대상이 된다.</summary>
public class OAuthTokenBizService(IOAuthTokenEntityService tokenEntityService, IMainTransaction transaction)
{
    public async Task<OAuthPruneResultDto> PruneAsync(DateTimeOffset now, int retentionDays, CancellationToken cancellationToken)
    {
        var threshold = OAuthTokenRetention.ThresholdOf(now, retentionDays);
        var tokens = await transaction.ExecuteAsync(token => tokenEntityService.PruneTokensAsync(threshold, token), cancellationToken);
        var authorizations = await transaction.ExecuteAsync(token => tokenEntityService.PruneAuthorizationsAsync(threshold, token), cancellationToken);
        return new OAuthPruneResultDto(tokens, authorizations);
    }
}
