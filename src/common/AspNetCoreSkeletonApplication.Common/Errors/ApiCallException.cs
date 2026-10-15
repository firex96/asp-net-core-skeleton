using System.Net;

namespace AspNetCoreSkeletonApplication.Common.Errors;

/// <summary>
/// 외부 API 호출이 실패했다 — 502. 상대가 오류 상태로 답했거나(재시도 후), 연결·타임아웃으로 답을 못 받았다.
/// 5xx 라 메시지는 응답에 나가지 않고 로그에만 남는다.
/// </summary>
public class ApiCallException(string message, HttpStatusCode? upstreamStatus, Exception? innerException)
    : CodedException(ErrorCode.ExternalApiError, message, innerException)
{
    /// <summary>상대가 돌려준 상태 코드. 응답을 못 받았으면 null.</summary>
    public HttpStatusCode? UpstreamStatus => upstreamStatus;
}
