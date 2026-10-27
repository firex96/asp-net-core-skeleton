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

    /// <summary>
    /// detail 을 상태에 맞춘다. 5xx 는 내부 사정이라 누가 넣었든 지운다(앱이 Problem(detail: …) 로 줘도).
    /// 문장이 없는 4xx(상태 코드 응답, 컨트롤러의 NotFound() · Forbid(), 프레임워크 예외)에는 상태별 사용자 문장을 싣는다.
    /// 검증 실패는 errors 가 문장이고, 이미 있는 4xx detail 은 그대로 둔다.
    /// </summary>
    public static void NormalizeDetail(this ProblemDetails problemDetails)
    {
        if (problemDetails.Status is >= StatusCodes.Status500InternalServerError)
        {
            problemDetails.Detail = null;
        }
        else if (problemDetails.Detail is null
            && problemDetails is not HttpValidationProblemDetails
            && problemDetails.Status is >= StatusCodes.Status400BadRequest)
        {
            problemDetails.Detail = ErrorMessages.ForStatus(problemDetails.Status.Value);
        }
    }
}
