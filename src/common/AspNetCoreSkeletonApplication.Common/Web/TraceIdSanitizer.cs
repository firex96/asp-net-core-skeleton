using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 밖에서 들어온 추적 ID 를 믿을 수 있는 형태로만 받는다. 검증 없이 로그에 넣으면 개행 주입으로 로그를 위조할 수 있고,
/// 나가는 호출을 타고 하위 서비스까지 번진다. 규칙은 Spring 형제 스켈레톤과 같다.
/// </summary>
public static partial class TraceIdSanitizer
{
    /// <summary>유효하면 그대로, 아니면 새 ID — 현재 W3C trace-id 가 있으면 그것, 없으면 새 GUID.</summary>
    public static string Sanitize(string? raw) =>
        IsValid(raw) ? raw! : Activity.Current?.TraceId.ToHexString() ?? Guid.NewGuid().ToString("N");

    public static bool IsValid(string? raw) => raw is not null && SafePattern().IsMatch(raw);

    // UUID · W3C trace-id 등 통용 형식을 받는 보수적인 허용 문자와 길이.
    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex SafePattern();
}
