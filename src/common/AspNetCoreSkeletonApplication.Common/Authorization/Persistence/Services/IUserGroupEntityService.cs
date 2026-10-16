using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Services;

/// <summary>사용자 그룹 엔티티 하나의 CRUD.</summary>
public interface IUserGroupEntityService
{
    /// <summary>없으면 <c>ItemNotFoundException</c>(404).</summary>
    Task<UserGroupDto> GetAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserGroupDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>같은 이름이 있으면 <c>ConflictException</c>(409).</summary>
    Task<UserGroupDto> CreateAsync(UserGroupSaveDto group, CancellationToken cancellationToken);

    Task<UserGroupDto> UpdateAsync(long id, UserGroupSaveDto group, CancellationToken cancellationToken);

    /// <summary>논리 삭제. 소속 · 역할 연결은 BizService 가 먼저 지운다.</summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken);
}
