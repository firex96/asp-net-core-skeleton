namespace AspNetCoreSkeletonApplication.Batch.Security;

/// <summary>Batch 앱의 인가 정책 이름. 정책 이름은 문자열로 흩뿌리지 않고 여기서만 쓴다.</summary>
public static class BatchPolicies
{
    /// <summary>웹훅 — API 키로 인증하고, 그 키에 허용된 Job 만 실행한다.</summary>
    public const string JobTrigger = "Batch.JobTrigger";
}
