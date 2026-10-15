namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>구성 경로 <c>Audit</c>.</summary>
public sealed class AuditOptions
{
    /// <summary>
    /// <c>X-User-Id</c> 헤더를 감사 주체로 믿을지. 기본은 false 다. 헤더는 외부 입력이라 그대로 믿으면 호출자가 다른 사용자로
    /// 감사 기록을 위조할 수 있다. 게이트웨이가 인증 결과로 헤더를 덮어쓰고 밖에서 온 같은 이름의 헤더를 지우는 배포에서만 켠다.
    /// </summary>
    public bool TrustUserHeader { get; set; }
}
