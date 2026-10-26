using System.ComponentModel.DataAnnotations;
using AspNetCoreSkeletonApplication.Common.Web;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace AspNetCoreSkeletonApplication.UnitTests.Web;

/// <summary>메시지를 지정하지 않은 검증 특성의 기본 문구 — 한국어이고 필드 이름을 넣지 않는다(필드 키 아래 보인다).</summary>
public class KoreanValidationMetadataProviderTests
{
    public static TheoryData<ValidationAttribute, string> Attributes => new()
    {
        { new RequiredAttribute(), "값을 넣어 주세요." },
        { new StringLengthAttribute(5), "5자 이하로 넣어 주세요." },
        { new StringLengthAttribute(5) { MinimumLength = 2 }, "2~5자로 넣어 주세요." },
        { new MaxLengthAttribute(3), "최대 3까지 넣을 수 있어요." },
        { new MinLengthAttribute(1), "최소 1 이상 넣어 주세요." },
        { new RangeAttribute(1, 10), "1 이상 10 이하로 넣어 주세요." },
        { new EmailAddressAttribute(), "형식이 올바르지 않아요." },
        { new CompareAttribute("Other"), "값이 올바르지 않아요." },
    };

    [Theory]
    [MemberData(nameof(Attributes))]
    public void DefaultMessage_IsKoreanWithoutFieldName(ValidationAttribute attribute, string expected)
    {
        attribute.ErrorMessage = KoreanValidationMetadataProvider.DefaultMessage(attribute);

        Assert.Equal(expected, attribute.FormatErrorMessage("Name"));
    }

    [Fact]
    public void CreateValidationMetadata_FillsOnlyAttributesWithoutMessage()
    {
        var withoutMessage = new RequiredAttribute();
        var withMessage = new StringLengthAttribute(5) { ErrorMessage = "이름은 다섯 자까지예요." };
        var context = new ValidationMetadataProviderContext(
            ModelMetadataIdentity.ForType(typeof(string)), ModelAttributes.GetAttributesForType(typeof(string)));
        context.ValidationMetadata.ValidatorMetadata.Add(withoutMessage);
        context.ValidationMetadata.ValidatorMetadata.Add(withMessage);

        new KoreanValidationMetadataProvider().CreateValidationMetadata(context);

        Assert.Equal("값을 넣어 주세요.", withoutMessage.ErrorMessage);
        Assert.Equal("이름은 다섯 자까지예요.", withMessage.ErrorMessage);
    }
}
