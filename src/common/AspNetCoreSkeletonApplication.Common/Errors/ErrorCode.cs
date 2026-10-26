namespace AspNetCoreSkeletonApplication.Common.Errors;

/// <summary>
/// API 에러 코드. 응답 ProblemDetails 의 <c>code</c> · <c>divisionCode</c> 확장 필드가 된다.
/// 코드 문자열과 구분 코드는 Spring 형제 스켈레톤과 같다 — 게이트웨이·클라이언트가 스택과 무관하게 같은 값을 본다.
/// 업무 코드가 필요하면 앱이나 Common 에 정적 필드를 더 둔다(생성자가 public 이다).
/// </summary>
public sealed class ErrorCode(string code, int status, string divisionCode, string message)
{
    public static readonly ErrorCode BadRequest = new("BAD_REQUEST_ERROR", 400, "G001", "Bad Request");
    public static readonly ErrorCode Unauthorized = new("UNAUTHORIZED_ERROR", 401, "G015", "Unauthorized");
    public static readonly ErrorCode Forbidden = new("FORBIDDEN_ERROR", 403, "G008", "Forbidden");
    public static readonly ErrorCode NotFound = new("NOT_FOUND_ERROR", 404, "G009", "Not Found");
    public static readonly ErrorCode NotValid = new("NOT_VALID_ERROR", 400, "G011", "One or more validation errors occurred");
    public static readonly ErrorCode Conflict = new("CONFLICT_ERROR", 409, "G013", "Conflict");
    public static readonly ErrorCode AccountLocked = new("ACCOUNT_LOCKED_ERROR", 423, "AUTH001", "Account Locked");
    public static readonly ErrorCode InvalidToken = new("INVALID_TOKEN_ERROR", 400, "AUTH002", "Invalid Token");
    public static readonly ErrorCode ExternalLoginNotLinked = new("EXTERNAL_LOGIN_NOT_LINKED_ERROR", 401, "AUTH003", "External Login Not Linked");
    public static readonly ErrorCode ExternalLoginInUse = new("EXTERNAL_LOGIN_IN_USE_ERROR", 409, "AUTH004", "External Login In Use");
    public static readonly ErrorCode LastLoginMethod = new("LAST_LOGIN_METHOD_ERROR", 409, "AUTH005", "Last Login Method");
    public static readonly ErrorCode EmailNotConfirmed = new("EMAIL_NOT_CONFIRMED_ERROR", 403, "AUTH006", "Email Not Confirmed");
    public static readonly ErrorCode ExternalApiError = new("EXTERNAL_API_ERROR", 502, "G014", "Bad Gateway");
    public static readonly ErrorCode ServiceUnavailable = new("SERVICE_UNAVAILABLE_ERROR", 503, "G016", "Service Unavailable");

    // 프레임워크가 정하는 상태(라우팅 405, 콘텐츠 협상 406 · 415, 요청 크기 413)의 코드 — FromStatus 가 고른다
    public static readonly ErrorCode MethodNotAllowed = new("METHOD_NOT_ALLOWED_ERROR", 405, "G017", "Method Not Allowed");
    public static readonly ErrorCode NotAcceptable = new("NOT_ACCEPTABLE_ERROR", 406, "G018", "Not Acceptable");
    public static readonly ErrorCode PayloadTooLarge = new("PAYLOAD_TOO_LARGE_ERROR", 413, "G019", "Payload Too Large");
    public static readonly ErrorCode UnsupportedMediaType = new("UNSUPPORTED_MEDIA_TYPE_ERROR", 415, "G020", "Unsupported Media Type");
    public static readonly ErrorCode InternalServerError = new("INTERNAL_SERVER_ERROR", 500, "G999", "Internal Server Error");

    private static readonly ErrorCode[] ByStatus =
    [
        BadRequest, Unauthorized, Forbidden, NotFound, MethodNotAllowed, NotAcceptable, Conflict, PayloadTooLarge,
        UnsupportedMediaType, AccountLocked, InternalServerError, ExternalApiError, ServiceUnavailable,
    ];

    public string Code => code;

    public int Status => status;

    public string DivisionCode => divisionCode;

    public string Message => message;

    /// <summary>
    /// 예외 없이 만들어진 응답(프레임워크의 404 · 405 · 415 등)에 붙일 코드를 상태 코드로 고른다. 목록에 없는 상태는 4xx 면
    /// <see cref="BadRequest"/>, 5xx 면 <see cref="InternalServerError"/> 이고, 그때는 응답 상태도 그 코드의 상태로 맞춘다
    /// (<c>ProblemDetailsExtensions.AddErrorCodeIfMissing</c>) — 상태와 코드가 어긋난 응답은 나가지 않는다.
    /// </summary>
    public static ErrorCode FromStatus(int status) =>
        ByStatus.FirstOrDefault(errorCode => errorCode.Status == status)
        ?? (status >= 500 ? InternalServerError : BadRequest);
}
