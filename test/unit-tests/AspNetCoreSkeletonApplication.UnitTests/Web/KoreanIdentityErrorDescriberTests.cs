using AspNetCoreSkeletonApplication.Common.Web;

namespace AspNetCoreSkeletonApplication.UnitTests.Web;

/// <summary>화면에 그대로 보이는 Identity 오류 — 문장은 한국어, 코드는 그대로(클라이언트 분기가 바뀌지 않게).</summary>
public class KoreanIdentityErrorDescriberTests
{
    private readonly KoreanIdentityErrorDescriber _describer = new();

    [Fact]
    public void PasswordRules_AreKorean_AndKeepTheirCodes()
    {
        var tooShort = _describer.PasswordTooShort(10);

        Assert.Equal("PasswordTooShort", tooShort.Code);
        Assert.Equal("비밀번호는 10자 이상이어야 해요.", tooShort.Description);
    }

    [Fact]
    public void DuplicateEmailAndUserName_ReadTheSame()
    {
        // 사용자 이름이 곧 이메일이라 둘이 함께 나온다 — 화면은 같은 문장을 한 번만 보인다.
        Assert.Equal(_describer.DuplicateEmail("a@example.com").Description, _describer.DuplicateUserName("a@example.com").Description);
    }
}
