using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 메시지를 지정하지 않은 DataAnnotations 검증 특성에 한국어 기본 문구를 준다(기본은 「The Name field is required.」 같은 영어).
/// 문구는 필드 키 아래 보이므로 필드 이름을 넣지 않는다. 메시지를 직접 지정한 특성은 건드리지 않는다.
/// DataAnnotations 메타데이터 제공자가 검증 특성을 넣은 뒤에 돌아야 하므로 <c>ModelMetadataDetailsProviders</c> 의 끝에 둔다.
/// </summary>
internal sealed class KoreanValidationMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (attribute.ErrorMessage is null && attribute.ErrorMessageResourceName is null)
            {
                attribute.ErrorMessage = DefaultMessage(attribute);
            }
        }
    }

    // 자리표시자 번호는 각 특성의 FormatErrorMessage 인자 순서다 — StringLength: 이름 · 최대 · 최소, Range: 이름 · 최소 · 최대, Min/MaxLength: 이름 · 길이
    internal static string DefaultMessage(ValidationAttribute attribute) => attribute switch
    {
        RequiredAttribute => ErrorMessages.ValueRequired,
        StringLengthAttribute { MinimumLength: > 0 } => "{2}~{1}자로 넣어 주세요.",
        StringLengthAttribute => "{1}자 이하로 넣어 주세요.",
        MaxLengthAttribute => "최대 {1}까지 넣을 수 있어요.",
        MinLengthAttribute => "최소 {1} 이상 넣어 주세요.",
        RangeAttribute => "{1} 이상 {2} 이하로 넣어 주세요.",
        EmailAddressAttribute or UrlAttribute or RegularExpressionAttribute or PhoneAttribute => "형식이 올바르지 않아요.",
        _ => "값이 올바르지 않아요.",
    };
}
