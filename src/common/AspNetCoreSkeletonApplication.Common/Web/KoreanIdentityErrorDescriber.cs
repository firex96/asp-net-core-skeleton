using Microsoft.AspNetCore.Identity;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// Identity 오류 메시지를 화면에 그대로 보여 줄 수 있는 한국어로. 코드(<see cref="IdentityError.Code"/>)는 그대로라 클라이언트 분기는 바뀌지 않는다.
/// 가입 · 재설정에서 실제로 나오는 것만 바꾸고, 나머지는 기본 문구를 쓴다.
/// </summary>
internal sealed class KoreanIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Error(nameof(DefaultError), "요청을 처리하지 못했어요. 잠시 후 다시 시도해 주세요.");

    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "비밀번호가 맞지 않아요.");

    public override IdentityError InvalidToken() => Error(nameof(InvalidToken), "링크가 맞지 않거나 이미 만료됐어요.");

    public override IdentityError InvalidEmail(string? email) => Error(nameof(InvalidEmail), "이메일 주소 형식이 올바르지 않아요.");

    public override IdentityError InvalidUserName(string? userName) => Error(nameof(InvalidUserName), "이메일 주소 형식이 올바르지 않아요.");

    public override IdentityError DuplicateEmail(string email) => Error(nameof(DuplicateEmail), "이미 가입된 이메일이에요.");

    // 사용자 이름이 곧 이메일이라 가입이 겹치면 DuplicateEmail 과 함께 나온다 — 화면은 같은 문장을 한 번만 보인다.
    public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), "이미 가입된 이메일이에요.");

    public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), $"비밀번호는 {length}자 이상이어야 해요.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Error(nameof(PasswordRequiresUniqueChars), $"서로 다른 문자를 {uniqueChars}개 이상 써 주세요.");

    public override IdentityError PasswordRequiresNonAlphanumeric() => Error(nameof(PasswordRequiresNonAlphanumeric), "특수 문자를 하나 이상 넣어 주세요.");

    public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit), "숫자를 하나 이상 넣어 주세요.");

    public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower), "영문 소문자를 하나 이상 넣어 주세요.");

    public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper), "영문 대문자를 하나 이상 넣어 주세요.");

    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };
}
