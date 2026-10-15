namespace AspNetCoreSkeletonApplication.Common.Errors;

/// <summary>찾는 대상이 없다 — 404. EntityService 의 <c>Get~</c> 이 대상이 없을 때 던진다.</summary>
public class ItemNotFoundException(string message) : CodedException(ErrorCode.NotFound, message, null);
