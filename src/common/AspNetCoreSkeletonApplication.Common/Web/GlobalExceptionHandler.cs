using AspNetCoreSkeletonApplication.Common.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 처리되지 않은 예외를 ProblemDetails 로 바꾼다. <see cref="CodedException"/> 은 그 에러 코드로,
/// <see cref="BadHttpRequestException"/>(요청 크기 초과 413, 최소 API 의 본문 형식 오류 400 등)은 그 상태의 코드로, 나머지는 500 으로 응답한다.
/// 5xx 는 내부 메시지를 내보내지 않고, 프레임워크 4xx 는 예외 메시지(매개변수 · 타입 이름이 들어간다) 대신 고정 문장을 쓴다.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // 업무 예외만 자기 메시지를 detail 로 싣는다 — 프레임워크 4xx 는 상태별 고정 문장(ErrorMessages)이다
        var (errorCode, message) = exception switch
        {
            CodedException coded => (coded.ErrorCode, coded.Message),
            BadHttpRequestException badRequest => (ErrorCode.FromStatus(badRequest.StatusCode), null),
            _ => (ErrorCode.InternalServerError, (string?)null),
        };

        if (errorCode.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "처리되지 않은 예외 — {Code}", errorCode.Code);
        }
        else
        {
            logger.LogWarning(exception, "요청 처리 실패 — {Code}", errorCode.Code);
        }

        var problemDetails = new ProblemDetails
        {
            Status = errorCode.Status,
            Title = errorCode.Message,
            Detail = errorCode.Status >= StatusCodes.Status500InternalServerError ? null : message ?? ErrorMessages.ForStatus(errorCode.Status),
        };
        problemDetails.AddErrorCode(errorCode);

        httpContext.Response.StatusCode = errorCode.Status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        });
    }
}
