using System.ComponentModel.DataAnnotations;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>구성 경로 <c>App</c>. 기동할 때 검사한다.</summary>
public sealed class AppOptions
{
    /// <summary>
    /// 사용자가 브라우저로 여는 SPA 의 출처(<c>https://shop.example.com</c>). 메일 속 링크를 이것으로 만든다 —
    /// 요청의 Host · Origin 헤더로 만들면 공격자가 헤더를 바꿔 남의 재설정 토큰을 자기 서버로 보낼 수 있다.
    /// </summary>
    [Required, Url]
    public string PublicOrigin { get; set; } = null!;
}
