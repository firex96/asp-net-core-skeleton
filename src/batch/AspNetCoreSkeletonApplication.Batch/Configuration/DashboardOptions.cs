namespace AspNetCoreSkeletonApplication.Batch.Configuration;

/// <summary>
/// 구성 경로 <c>Batch:Dashboard</c> — Quartz 대시보드(<c>/quartz</c>)의 운영자 계정. 브라우저가 띄우는 Basic 인증으로 들어온다.
/// <see cref="Password"/> 는 비밀값이다. 계정을 적지 않으면 아무도 들어오지 못한다(fail-closed).
/// </summary>
public sealed class DashboardOptions
{
    public string Username { get; set; } = "";

    public string Password { get; set; } = "";
}
