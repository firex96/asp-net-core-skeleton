namespace AspNetCoreSkeletonApplication.Batch.Configuration;

/// <summary>
/// 구성 경로 <c>Batch:Jobs:{Job 클래스 이름}</c>. 스케줄은 여기서만 정한다 — 코드에 cron 을 적지 않는다.
/// </summary>
public sealed class JobScheduleOptions
{
    /// <summary>false 면 트리거를 두지 않는다. Job 은 남아 있어 수동 실행(웹훅 · 대시보드)은 된다.</summary>
    public bool Enabled { get; set; }

    /// <summary>Quartz cron 식(초 포함 6~7자리). <see cref="Enabled"/> 면 필수다.</summary>
    public string? Cron { get; set; }

    /// <summary>수동 실행 때 받을 수 있는 파라미터 이름. 비어 있으면 아무것도 받지 않는다.</summary>
    public string[] AllowedParameters { get; set; } = [];
}
