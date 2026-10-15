using AspNetCoreSkeletonApplication.Common.Constants;

namespace AspNetCoreSkeletonApplication.Common.Contexts;

/// <summary>
/// 현재 실행의 감사 주체(사용자 ID). AsyncLocal 이라 await 를 따라 흐른다. HTTP 요청에서는 AuditorContextMiddleware 가,
/// 배치 같은 비 HTTP 실행에서는 그 진입점이 <see cref="Begin"/> 으로 채운다.
/// 로깅 scope 와 채널을 나눈다 — 로그 컨텍스트는 비어도 로그가 밋밋해질 뿐이지만, 감사 주체가 틀리면 DB 에 틀린 값이 남는다.
/// </summary>
public static class AuditorContext
{
    private static readonly AsyncLocal<long?> CurrentValue = new();

    /// <summary>정해진 감사 주체. 없으면 null.</summary>
    public static long? Current => CurrentValue.Value;

    /// <summary>감사 필드에 기록할 값 — 정해진 주체, 없으면 <see cref="AuditorIds.System"/>.</summary>
    public static long CurrentOrSystem => CurrentValue.Value ?? AuditorIds.System;

    /// <summary>범위가 끝나면 이전 값으로 돌아간다 — 다음 실행이 남의 ID 로 감사되지 않게 한다.</summary>
    public static IDisposable Begin(long? userId)
    {
        var previous = CurrentValue.Value;
        CurrentValue.Value = userId;
        return new Scope(previous);
    }

    private sealed class Scope(long? previous) : IDisposable
    {
        public void Dispose() => CurrentValue.Value = previous;
    }
}
