using AspNetCoreSkeletonApplication.Common.Account.Dtos;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Riok.Mapperly.Abstractions;

namespace AspNetCoreSkeletonApplication.Common.Account.Persistence.Mappers;

/// <summary>
/// Identity 계정 → Dto. <see cref="ApplicationUser"/> 는 프레임워크가 정의한 많은 속성(해시 · 스탬프 · 잠금 …)을 갖고, 그중 둘만 내보낸다 —
/// 그래서 이 매퍼만 대상 기준으로 검사한다(소스 쪽 남는 멤버를 하나하나 적지 않는다).
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
internal static partial class UserAccountEntityMapper
{
    public static partial IQueryable<UserAccountDto> ProjectToDto(this IQueryable<ApplicationUser> query);
}
