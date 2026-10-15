namespace AspNetCoreSkeletonApplication.Common.Diagnostics;

/// <summary>구성 경로 <c>Diagnostics</c>. 넘으면 경고 로그를 남기는 임계값들.</summary>
public sealed class DiagnosticsOptions
{
    /// <summary>DB 명령 하나의 실행 시간.</summary>
    public TimeSpan SlowQueryThreshold { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>HTTP 요청 하나의 처리 시간(응답을 쓰기까지).</summary>
    public TimeSpan SlowRequestThreshold { get; set; } = TimeSpan.FromSeconds(1);
}
