using System.Net.Http.Json;
using AspNetCoreSkeletonApplication.Common.Errors;

namespace AspNetCoreSkeletonApplication.Common.Web;

public static class HttpResponseMessageExtensions
{
    /// <summary>
    /// 성공 응답의 JSON 본문을 읽는다. 오류 상태이거나 본문이 비어 있으면 <see cref="ApiCallException"/> 을 던진다 —
    /// 클라이언트가 실패를 조용히 null 로 넘기지 않게 한다.
    /// </summary>
    public static async Task<T> ReadRequiredFromJsonAsync<T>(
        this HttpResponseMessage response, string clientName, CancellationToken cancellationToken)
    {
        var request = response.RequestMessage;
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiCallException(
                $"{clientName} {request?.Method} {request?.RequestUri} 실패 — {(int)response.StatusCode}", response.StatusCode, null);
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new ApiCallException(
                $"{clientName} {request?.Method} {request?.RequestUri} 본문이 비어 있다", response.StatusCode, null);
    }
}
