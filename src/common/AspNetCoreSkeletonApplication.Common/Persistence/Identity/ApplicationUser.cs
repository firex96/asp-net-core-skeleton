using Microsoft.AspNetCore.Identity;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Identity;

/// <summary>
/// 로그인 계정 — ASP.NET Core Identity 가 소유한다(프레임워크 정본 스키마라 업무 엔티티 규칙의 대상이 아니다). 식별자는 <c>long</c> 이고,
/// 로그인한 요청의 감사 주체가 이 Id 다. 업무 데이터는 여기에 두지 않고 사용자 ID 로 가리키는 별도 엔티티에 둔다.
/// </summary>
internal sealed class ApplicationUser : IdentityUser<long>;
