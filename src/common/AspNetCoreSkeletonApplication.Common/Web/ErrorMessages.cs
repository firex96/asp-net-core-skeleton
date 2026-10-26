namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 프레임워크가 만든 4xx 에 싣는 사용자 문장. 요청 값 · 타입 이름 · 예외 메시지를 싣지 않는다 — 원래 내용은 로그에 남는다.
/// 업무 실패의 문장은 <c>CodedException</c> 의 메시지다.
/// </summary>
internal static class ErrorMessages
{
    public const string InvalidFormat = "값의 형식이 올바르지 않아요.";
    public const string UnsupportedCode = "지원하지 않는 값이에요.";
    public const string ValueRequired = "값을 넣어 주세요.";
    public const string NumberRequired = "숫자를 넣어 주세요.";
    public const string BodyRequired = "요청 본문이 없어요.";

    public static string ForStatus(int status) => status switch
    {
        405 => "이 주소에서 지원하지 않는 요청 방식이에요.",
        406 => "요청한 응답 형식을 제공할 수 없어요.",
        413 => "요청 본문이 너무 커요.",
        415 => "지원하지 않는 요청 본문 형식이에요.",
        _ => "요청을 처리할 수 없어요. 보낸 값을 확인해 주세요.",
    };
}
