using AspNetCoreSkeletonApplication.Common.Errors;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 재시도까지 끝난 뒤에도 응답을 못 받은 실패(연결 오류, 타임아웃, 회로 차단)를 <see cref="ApiCallException"/> 으로 바꾼다.
/// 클라이언트 핸들러 체인의 가장 바깥에 둔다. 취소는 바꾸지 않는다.
/// </summary>
internal sealed class ApiCallExceptionHandler(string clientName) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException
                                              or TimeoutRejectedException
                                              or BrokenCircuitException)
        {
            throw new ApiCallException(
                $"{clientName} {request.Method} {request.RequestUri} 응답 없음 — {exception.GetType().Name}", null, exception);
        }
    }
}
