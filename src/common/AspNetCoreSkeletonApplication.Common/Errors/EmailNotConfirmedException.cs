namespace AspNetCoreSkeletonApplication.Common.Errors;

/// <summary>이메일을 확인하지 않은 계정의 로그인 — 403 <c>EMAIL_NOT_CONFIRMED_ERROR</c>. 비밀번호가 맞았을 때만 던진다(계정 존재를 드러내지 않는다).</summary>
public class EmailNotConfirmedException(string message) : CodedException(ErrorCode.EmailNotConfirmed, message, null);
