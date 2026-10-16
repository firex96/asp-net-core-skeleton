namespace AspNetCoreSkeletonApplication.Common.Errors;

/// <summary>비밀번호 재설정 같은 1회용 토큰이 틀렸거나 만료됐거나 이미 쓰였다 — 400 <c>INVALID_TOKEN_ERROR</c>. 어느 경우인지 구분해 알리지 않는다.</summary>
public class InvalidTokenException(string message) : CodedException(ErrorCode.InvalidToken, message, null);
