namespace AspNetCoreSkeletonApplication.Common.Errors;

/// <summary>현재 상태와 충돌한다 — 409. 허용되지 않은 상태 전이, 중복 생성 등.</summary>
public class ConflictException(string message) : CodedException(ErrorCode.Conflict, message, null);
