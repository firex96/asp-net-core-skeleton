using System.ComponentModel.DataAnnotations;

namespace AspNetCoreSkeletonApplication.Batch.Configuration;

/// <summary>구성 경로 <c>Batch:Jobs:DiscontinueStaleDraftJob</c> — 스케줄(<see cref="JobScheduleOptions"/>)과 같은 섹션이다.</summary>
public sealed class DiscontinueStaleDraftJobOptions
{
    /// <summary>마지막 수정 뒤 이만큼 지난 Draft 상품을 판매 종료한다.</summary>
    [Range(1, 3650)]
    public int StaleAfterDays { get; set; } = 30;

    /// <summary>한 트랜잭션에서 처리할 상품 수.</summary>
    [Range(1, 1000)]
    public int PageSize { get; set; } = 100;
}
