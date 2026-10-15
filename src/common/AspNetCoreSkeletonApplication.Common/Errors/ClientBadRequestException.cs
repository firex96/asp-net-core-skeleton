namespace AspNetCoreSkeletonApplication.Common.Errors;

/// <summary>요청이 업무 규칙상 받아들일 수 없다 — 400. 형식 검증 실패는 DataAnnotations 가 먼저 막는다.</summary>
public class ClientBadRequestException(string message) : CodedException(ErrorCode.BadRequest, message, null);
