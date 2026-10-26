using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// <c>[ApiController]</c> 자동 400 의 응답을 만든다. 예외에서 온 모델 오류(JSON 입력 오류 — 메시지가 비어 있다)는
/// 예외 메시지 대신 고정 문장으로 바꾼다. 그 밖은 프레임워크 기본 팩터리와 같다(코드 · traceId 는 <c>CustomizeProblemDetails</c> 가 붙인다).
/// </summary>
internal static class UserFacingValidationResponse
{
    public static IActionResult Create(ActionContext context)
    {
        var modelState = new ModelStateDictionary();
        foreach (var (key, entry) in context.ModelState)
        {
            foreach (var error in entry.Errors)
            {
                modelState.AddModelError(key, string.IsNullOrEmpty(error.ErrorMessage) ? ErrorMessages.InvalidFormat : error.ErrorMessage);
            }
        }

        var problemDetails = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
            .CreateValidationProblemDetails(context.HttpContext, modelState);
        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json", "application/problem+xml" },
        };
    }
}
