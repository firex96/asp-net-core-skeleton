using System.ComponentModel.DataAnnotations;

namespace AspNetCoreSkeletonApplication.Batch.Configuration;

/// <summary>구성 경로 <c>Batch:Jobs:PruneOAuthTokenJob</c> — 스케줄(<see cref="JobScheduleOptions"/>)과 같은 섹션이다.</summary>
public sealed class PruneOAuthTokenJobOptions
{
    /// <summary>만들어진 지 이만큼 지난, 더 쓸 수 없는 토큰 · 동의를 지운다.</summary>
    [Range(1, 3650)]
    public int RetentionDays { get; set; } = 14;
}
