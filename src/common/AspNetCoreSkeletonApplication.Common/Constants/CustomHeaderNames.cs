namespace AspNetCoreSkeletonApplication.Common.Constants;

/// <summary>스켈레톤이 읽고 쓰는 HTTP 헤더. 이름은 Spring 형제 스켈레톤과 같다.</summary>
public static class CustomHeaderNames
{
    /// <summary>요청 추적 ID. 받아서 검증해 쓰고, 응답과 나가는 호출에 그대로 싣는다.</summary>
    public const string TraceId = "X-Trace-Id";
}
