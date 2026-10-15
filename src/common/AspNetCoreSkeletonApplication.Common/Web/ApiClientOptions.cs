using System.ComponentModel.DataAnnotations;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 외부 API 클라이언트 구성의 기반. 클라이언트마다 상속한 <c>~Options</c> 를 두고 구성 경로를 정한다
/// (예: <c>Clients:ExampleApi</c>). 값이 없으면 기동이 멈춘다. 재시도·타임아웃은 표준 resilience 기본값을 쓴다.
/// </summary>
public abstract class ApiClientOptions
{
    /// <summary>끝에 <c>/</c> 를 둔다 — 클라이언트는 상대 경로(<c>status</c>)로 호출한다.</summary>
    [Required]
    public Uri? BaseAddress { get; set; }
}
