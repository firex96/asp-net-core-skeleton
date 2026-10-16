namespace AspNetCoreSkeletonApplication.Common.Account.Dtos;

/// <summary>로그인 계정의 공개 가능한 부분. 비밀번호 해시 · 보안 스탬프 같은 Identity 내부 값은 담지 않는다.</summary>
public sealed record UserAccountDto(long Id, string Email);
