namespace AspNetCoreSkeletonApplication.Batch.Configuration;

/// <summary>
/// 웹훅 API 키 하나. <see cref="Key"/> 는 비밀값이다 — 환경변수(<c>Batch__Webhook__ApiKeys__0__Key</c>)나 비밀 저장소로만 넣는다.
/// 키마다 실행할 수 있는 Job 을 <see cref="AllowedJobs"/> 로 적는다. 비어 있으면 아무 Job 도 실행할 수 없다(fail-closed).
/// </summary>
public sealed class WebhookApiKeyOptions
{
    /// <summary>로그와 감사에 남는 키 이름. 키 값은 어디에도 남지 않는다.</summary>
    public string Name { get; set; } = "";

    public string Key { get; set; } = "";

    public string[] AllowedJobs { get; set; } = [];
}
