using AspNetCoreSkeletonApplication.Common.Constants;
using AspNetCoreSkeletonApplication.Common.Contexts;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>나가는 호출에 현재 요청의 추적 ID 를 <c>X-Trace-Id</c> 로 싣는다. 이미 있으면 덮지 않는다.</summary>
internal sealed class TraceIdPropagationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var traceId = TraceIdContext.Current;
        if (traceId is not null && !request.Headers.Contains(CustomHeaderNames.TraceId))
        {
            request.Headers.Add(CustomHeaderNames.TraceId, traceId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
