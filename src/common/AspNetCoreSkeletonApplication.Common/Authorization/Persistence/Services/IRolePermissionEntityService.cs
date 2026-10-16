using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Services;

/// <summary>역할 ↔ 권한 코드 엔티티 하나의 CRUD.</summary>
public interface IRolePermissionEntityService
{
    /// <summary>전체 목록. 작은 표라 통째로 캐시하고 요청마다 권한 판정에 쓴다.</summary>
    Task<IReadOnlyList<RolePermissionDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>역할의 권한을 주어진 코드 집합으로 바꾼다(없는 것은 더하고 빠진 것은 지운다).</summary>
    Task ReplaceAsync(long roleId, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken);

    /// <summary>권한을 가진 역할. 트랜잭션 안의 검사용이라 캐시하지 않는다.</summary>
    Task<IReadOnlyList<long>> ListRoleIdsWithPermissionAsync(string permission, CancellationToken cancellationToken);
}
