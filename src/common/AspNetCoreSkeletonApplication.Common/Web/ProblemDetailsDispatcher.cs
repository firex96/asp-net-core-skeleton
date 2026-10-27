using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 기본 <see cref="IProblemDetailsService"/> 를 대신한다. 등록된 writer 를 등록 순서대로 골라 쓰는 것은 같고, 맞는 writer 가 없을 때만 다르다.
/// 기본 서비스는 그때(Accept 가 JSON 을 받지 않는 요청 — <c>Accept: text/html</c> 만) 실패를 돌려준다. 그러면 상태 코드 페이지는 plain text 를 쓰고,
/// 최소 API 의 <c>TypedResults.Problem</c> 은 <c>CustomizeProblemDetails</c> 를 거치지 않은 원래 객체를 JSON 으로 쓴다 — 코드가 없고 5xx detail 이 남는다.
/// 여기서는 같은 customize 를 거친 <c>application/problem+json</c> 으로 쓴다. 기본 writer 가 붙이는 RFC <c>type</c> 링크는
/// 프레임워크 내부 표라 대비 경로에는 없다 — 코드 · 문장 · 5xx 규칙은 같다.
/// </summary>
internal sealed class ProblemDetailsDispatcher(
    IEnumerable<IProblemDetailsWriter> writers,
    IOptions<ProblemDetailsOptions> options,
    IOptions<HttpJsonOptions> jsonOptions) : IProblemDetailsService
{
    private readonly IProblemDetailsWriter[] _writers = writers.ToArray();

    public async ValueTask WriteAsync(ProblemDetailsContext context)
    {
        if (!await TryWriteAsync(context))
        {
            throw new InvalidOperationException("응답이 이미 시작돼 ProblemDetails 를 쓸 수 없다.");
        }
    }

    public async ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
    {
        foreach (var writer in _writers)
        {
            if (writer.CanWrite(context))
            {
                await writer.WriteAsync(context);
                return true;
            }
        }

        var problemDetails = context.ProblemDetails;
        var response = context.HttpContext.Response;
        // 이미 시작된 응답에는 쓸 수 없다 — 기본 서비스처럼 실패를 돌려준다
        if (response.HasStarted)
        {
            return false;
        }

        problemDetails.Status ??= response.StatusCode;
        problemDetails.Title ??= ReasonPhrases.GetReasonPhrase(problemDetails.Status.Value);
        options.Value.CustomizeProblemDetails?.Invoke(context);

        await response.WriteAsJsonAsync(
            problemDetails,
            jsonOptions.Value.SerializerOptions.GetTypeInfo(problemDetails.GetType()),
            "application/problem+json",
            context.HttpContext.RequestAborted);
        return true;
    }
}
