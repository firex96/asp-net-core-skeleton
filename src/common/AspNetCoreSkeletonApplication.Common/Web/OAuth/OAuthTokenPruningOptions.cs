using System.ComponentModel.DataAnnotations;

namespace AspNetCoreSkeletonApplication.Common.Web.OAuth;

/// <summary>
/// 구성 경로 <c>Authentication:OAuthServer:TokenPruning</c> — 만료 · 폐기 토큰과 쓰이지 않는 동의를 Server 가 직접 정리할지.
/// 기본은 Batch 의 <c>PruneOAuthTokenJob</c> 이 정리하므로 꺼 둔다. Batch 없이 배포하면(템플릿 <c>--batch false</c>) 켠다.
/// </summary>
public sealed class OAuthTokenPruningOptions
{
    public bool RunInServer { get; set; }

    /// <summary>정리 주기. 기동하자마자 한 번, 그 뒤로 이 간격마다.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "7.00:00:00")]
    public TimeSpan Interval { get; set; } = TimeSpan.FromDays(1);

    /// <summary>만들어진 지 이만큼 지난, 더 쓸 수 없는 토큰 · 동의를 지운다(Batch Job 과 같은 규칙).</summary>
    [Range(1, 3650)]
    public int RetentionDays { get; set; } = 14;
}
