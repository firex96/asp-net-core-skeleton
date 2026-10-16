namespace AspNetCoreSkeletonApplication.Common.Errors;

/// <summary>남은 로그인 수단을 모두 없애는 변경 — 409 <c>LAST_LOGIN_METHOD_ERROR</c>.</summary>
public class LastLoginMethodException(string message) : CodedException(ErrorCode.LastLoginMethod, message, null);
