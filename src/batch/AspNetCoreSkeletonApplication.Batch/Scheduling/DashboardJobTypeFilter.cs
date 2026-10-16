namespace AspNetCoreSkeletonApplication.Batch.Scheduling;

/// <summary>
/// 운영 대시보드가 이름으로 다룰 수 있는 Job 타입 — 이 앱의 도메인 <c>Jobs</c> 네임스페이스(<c>{앱}.{도메인}.Jobs</c>)에 있는 타입만.
/// 임의 타입(예: 실행 파일을 띄우는 Quartz 의 <c>NativeJob</c>)으로 Job 을 만들지 못하게 막는다.
/// </summary>
public static class DashboardJobTypeFilter
{
    private static readonly string AppNamespace = typeof(Program).Namespace! + ".";

    /// <param name="typeName">타입 이름. 어셈블리 이름이 붙어 있어도(<c>Ns.Type, Assembly</c>) 된다.</param>
    public static bool IsAllowed(string typeName)
    {
        var name = typeName.Split(',')[0].Trim();
        var lastDot = name.LastIndexOf('.');
        return lastDot > 0
               && name.StartsWith(AppNamespace, StringComparison.Ordinal)
               && name[..lastDot].EndsWith(".Jobs", StringComparison.Ordinal);
    }
}
