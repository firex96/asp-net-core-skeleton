namespace AspNetCoreSkeletonApplication.Common.Errors;

/// <summary>
/// 에러 코드를 가진 예외의 기반. 던지면 GlobalExceptionHandler 가 <see cref="ErrorCode"/> 의 상태 코드와
/// ProblemDetails 로 응답한다. 메시지는 4xx 응답의 <c>detail</c> 로 그대로 나가므로 사용자에게 보여도 되는 문장으로 쓴다.
/// </summary>
public abstract class CodedException(ErrorCode errorCode, string message, Exception? innerException)
    : Exception(message, innerException)
{
    public ErrorCode ErrorCode => errorCode;
}
