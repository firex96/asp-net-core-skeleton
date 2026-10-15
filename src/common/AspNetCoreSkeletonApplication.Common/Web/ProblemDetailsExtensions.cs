using AspNetCoreSkeletonApplication.Common.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreSkeletonApplication.Common.Web;

internal static class ProblemDetailsExtensions
{
    public const string CodeKey = "code";
    public const string DivisionCodeKey = "divisionCode";

    public static void AddErrorCode(this ProblemDetails problemDetails, ErrorCode errorCode)
    {
        problemDetails.Extensions[CodeKey] = errorCode.Code;
        problemDetails.Extensions[DivisionCodeKey] = errorCode.DivisionCode;
    }

    /// <summary>
    /// 예외 없이 만들어진 ProblemDetails(검증 실패 400, 컨트롤러의 NotFound() 등)에도 같은 확장 필드를 붙인다.
    /// 이미 코드가 있으면(GlobalExceptionHandler 가 만든 응답) 그대로 둔다.
    /// </summary>
    public static void AddErrorCodeIfMissing(this ProblemDetails problemDetails)
    {
        if (problemDetails.Extensions.ContainsKey(CodeKey))
        {
            return;
        }

        var errorCode = problemDetails is ValidationProblemDetails
            ? ErrorCode.NotValid
            : ErrorCode.FromStatus(problemDetails.Status ?? 500);
        problemDetails.AddErrorCode(errorCode);
    }
}
