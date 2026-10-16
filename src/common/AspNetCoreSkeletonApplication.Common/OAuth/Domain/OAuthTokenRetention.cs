namespace AspNetCoreSkeletonApplication.Common.OAuth.Domain;

/// <summary>만료 · 폐기된 토큰과 쓰이지 않는 동의를 얼마나 남겨 두나 — 기준 시각보다 먼저 만들어진 것만 지운다(감사 · 문제 추적용 여유).</summary>
public static class OAuthTokenRetention
{
    public static DateTimeOffset ThresholdOf(DateTimeOffset now, int retentionDays) =>
        retentionDays >= 1
            ? now.AddDays(-retentionDays)
            : throw new ArgumentOutOfRangeException(nameof(retentionDays), retentionDays, "보관 기간은 1일 이상이다");
}
