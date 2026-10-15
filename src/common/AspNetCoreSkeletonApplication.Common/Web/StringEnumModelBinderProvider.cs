using AspNetCoreSkeletonApplication.Common.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 쿼리 · 라우트 · 폼의 열거형 값을 외부 코드로 읽는다(<see cref="EnumCodes"/>) — JSON 본문과 같은 코드를 쓴다.
/// 모르는 코드는 모델 오류가 되어 400(NOT_VALID_ERROR)으로 응답한다. 본문은 JSON 변환기가 맡으므로 건드리지 않는다.
/// </summary>
internal sealed class StringEnumModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var enumType = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return enumType.IsEnum && context.BindingInfo.BindingSource != BindingSource.Body
            ? new Binder(enumType)
            : null;
    }

    private sealed class Binder(Type enumType) : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var result = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (result == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, result);
            if (EnumCodes.TryParse(enumType, result.FirstValue, out var value))
            {
                bindingContext.Result = ModelBindingResult.Success(value);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName, $"'{result.FirstValue}' 는 {enumType.Name} 의 코드가 아니다");
            }

            return Task.CompletedTask;
        }
    }
}
