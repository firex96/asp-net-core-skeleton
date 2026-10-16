using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Services;

/// <summary>그룹 ↔ 역할 연결 엔티티 하나의 CRUD.</summary>
public interface IUserGroupRoleEntityService
{
    /// <summary>전체 연결. 작은 표라 통째로 캐시하고 요청마다 권한 판정에 쓴다.</summary>
    Task<IReadOnlyList<UserGroupRoleDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>이미 있으면 아무것도 하지 않는다.</summary>
    Task AddAsync(long userGroupId, long roleId, CancellationToken cancellationToken);

    /// <summary>연결이 없으면 <c>ItemNotFoundException</c>(404).</summary>
    Task RemoveAsync(long userGroupId, long roleId, CancellationToken cancellationToken);

    Task RemoveAllOfGroupAsync(long userGroupId, CancellationToken cancellationToken);

    Task RemoveAllOfRoleAsync(long roleId, CancellationToken cancellationToken);

    /// <summary>역할들 중 하나라도 가진 그룹. 트랜잭션 안의 검사용이라 캐시하지 않는다.</summary>
    Task<IReadOnlyList<long>> ListGroupIdsWithRolesAsync(IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken);
}
