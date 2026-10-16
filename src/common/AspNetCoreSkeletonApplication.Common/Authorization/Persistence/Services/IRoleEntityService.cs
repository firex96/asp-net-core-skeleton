using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Services;

/// <summary>역할 엔티티 하나의 CRUD.</summary>
public interface IRoleEntityService
{
    /// <summary>없으면 <c>ItemNotFoundException</c>(404).</summary>
    Task<RoleDto> GetAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>같은 이름이 있으면 <c>ConflictException</c>(409).</summary>
    Task<RoleDto> CreateAsync(RoleSaveDto role, CancellationToken cancellationToken);

    Task<RoleDto> UpdateAsync(long id, RoleSaveDto role, CancellationToken cancellationToken);

    /// <summary>논리 삭제. 그룹 연결 · 권한은 BizService 가 먼저 지운다.</summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken);
}
