using AspNetCoreSkeletonApplication.Common.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.Common.Web;

internal static class ProblemDetailsExtensions
{
    public const string CodeKey = "code";
    public const string DivisionCodeKey = "divisionCode";
    public const string TraceIdKey = "traceId";

    public static void AddErrorCode(this ProblemDetails problemDetails, ErrorCode errorCode)
    {
        problemDetails.Extensions[CodeKey] = errorCode.Code;
        problemDetails.Extensions[DivisionCodeKey] = errorCode.DivisionCode;
    }

    /// <summary>
    /// 예외 없이 만들어진 ProblemDetails(검증 실패 400, 컨트롤러의 NotFound() 등)에도 같은 확장 필드를 붙인다.
    /// 이미 코드가 있으면(GlobalExceptionHandler 가 만든 응답) 그대로 둔다. 코드가 없는 상태(418 등)는 대체 코드를 붙이고
    /// 응답 상태 · <c>status</c> · <c>title</c> 을 그 코드의 값으로 맞춘다.
    /// </summary>
    public static void AddErrorCodeIfMissing(this ProblemDetails problemDetails, HttpContext httpContext)
    {
        if (problemDetails.Extensions.ContainsKey(CodeKey))
        {
            return;
        }

        // HttpValidationProblemDetails 는 MVC 의 ValidationProblemDetails 와 최소 API 의 ValidationProblem 이 함께 쓰는 기반 타입이다.
        var errorCode = problemDetails is HttpValidationProblemDetails
            ? ErrorCode.NotValid
            : ErrorCode.FromStatus(problemDetails.Status ?? 500);
        if (problemDetails.Status != errorCode.Status)
        {
            problemDetails.Status = errorCode.Status;
            problemDetails.Title = errorCode.Message;
            problemDetails.Type = null;
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = errorCode.Status;
            }
        }

        problemDetails.AddErrorCode(errorCode);
    }
}
