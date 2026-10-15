namespace AspNetCoreSkeletonApplication.Common.Contexts;

/// <summary>
/// 현재 요청의 추적 ID. AsyncLocal 이라 await 와 Task.Run 을 따라 흐른다 — 별도 전달 장치가 필요 없다.
/// HTTP 요청에서는 TraceIdMiddleware 가 채운다.
/// </summary>
public static class TraceIdContext
{
    private static readonly AsyncLocal<string?> CurrentValue = new();

    public static string? Current => CurrentValue.Value;

    /// <summary>범위가 끝나면 이전 값으로 돌아간다.</summary>
    public static IDisposable Begin(string traceId)
    {
        var previous = CurrentValue.Value;
        CurrentValue.Value = traceId;
        return new Scope(previous);
    }

    private sealed class Scope(string? previous) : IDisposable
    {
        public void Dispose() => CurrentValue.Value = previous;
    }
}
