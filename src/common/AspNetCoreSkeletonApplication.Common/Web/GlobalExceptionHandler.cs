using AspNetCoreSkeletonApplication.Common.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 처리되지 않은 예외를 ProblemDetails 로 바꾼다. <see cref="CodedException"/> 은 그 에러 코드로,
/// 나머지는 500 으로 응답한다. 5xx 는 내부 메시지를 내보내지 않고 고정 문구를 쓴다.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var errorCode = exception is CodedException coded ? coded.ErrorCode : ErrorCode.InternalServerError;

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
            Detail = errorCode.Status >= StatusCodes.Status500InternalServerError ? null : exception.Message,
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
