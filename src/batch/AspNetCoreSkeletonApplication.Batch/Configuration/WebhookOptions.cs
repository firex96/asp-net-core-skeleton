namespace AspNetCoreSkeletonApplication.Batch.Configuration;

/// <summary>구성 경로 <c>Batch:Webhook</c> — 수동 실행 웹훅의 API 키 목록.</summary>
public sealed class WebhookOptions
{
    public List<WebhookApiKeyOptions> ApiKeys { get; set; } = [];
}
