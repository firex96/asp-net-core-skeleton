using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 모델 바인딩 오류 문구를 화면에 그대로 보여 줄 수 있는 한국어로. 문구는 필드 키 아래 보이므로 필드 이름을 넣지 않고,
/// 요청 값(시도한 값)도 되돌려 싣지 않는다.
/// </summary>
internal static class KoreanModelBindingMessages
{
    public static void Apply(DefaultModelBindingMessageProvider provider)
    {
        provider.SetMissingBindRequiredValueAccessor(_ => ErrorMessages.ValueRequired);
        provider.SetMissingKeyOrValueAccessor(() => ErrorMessages.ValueRequired);
        provider.SetMissingRequestBodyRequiredValueAccessor(() => ErrorMessages.BodyRequired);
        provider.SetValueMustNotBeNullAccessor(_ => ErrorMessages.ValueRequired);
        provider.SetAttemptedValueIsInvalidAccessor((_, _) => ErrorMessages.InvalidFormat);
        provider.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => ErrorMessages.InvalidFormat);
        provider.SetUnknownValueIsInvalidAccessor(_ => ErrorMessages.InvalidFormat);
        provider.SetNonPropertyUnknownValueIsInvalidAccessor(() => ErrorMessages.InvalidFormat);
        provider.SetValueIsInvalidAccessor(_ => ErrorMessages.InvalidFormat);
        provider.SetValueMustBeANumberAccessor(_ => ErrorMessages.NumberRequired);
        provider.SetNonPropertyValueMustBeANumberAccessor(() => ErrorMessages.NumberRequired);
    }
}
