namespace AspNetCoreSkeletonApplication.Batch.Configuration;

/// <summary>구성 경로 <c>Batch</c>.</summary>
public sealed class BatchOptions
{
    /// <summary>Job 클래스 이름 → 스케줄 · 수동 실행 설정. Job 고유 설정(예: StaleAfterDays)은 같은 섹션에 있어도 여기서는 읽지 않는다.</summary>
    public Dictionary<string, JobScheduleOptions> Jobs { get; set; } = [];

    public WebhookOptions Webhook { get; set; } = new();
}
