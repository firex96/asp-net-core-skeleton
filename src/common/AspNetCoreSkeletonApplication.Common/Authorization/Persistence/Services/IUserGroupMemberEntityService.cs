using AspNetCoreSkeletonApplication.Common.Authorization.Dtos;

namespace AspNetCoreSkeletonApplication.Common.Authorization.Persistence.Services;

/// <summary>그룹 소속 엔티티 하나의 CRUD.</summary>
public interface IUserGroupMemberEntityService
{
    Task<IReadOnlyList<UserGroupMemberDto>> ListByGroupAsync(long userGroupId, CancellationToken cancellationToken);

    /// <summary>사용자가 속한 그룹. 요청마다 권한을 판정할 때 쓰므로 캐시한다.</summary>
    Task<IReadOnlyList<long>> ListGroupIdsOfUserAsync(long userId, CancellationToken cancellationToken);

    /// <summary>이미 속해 있으면 아무것도 하지 않는다.</summary>
    Task AddAsync(long userGroupId, long userId, CancellationToken cancellationToken);

    /// <summary>속해 있지 않으면 <c>ItemNotFoundException</c>(404).</summary>
    Task RemoveAsync(long userGroupId, long userId, CancellationToken cancellationToken);

    Task RemoveAllOfGroupAsync(long userGroupId, CancellationToken cancellationToken);

    /// <summary>그룹들에 속한 서로 다른 사용자 수. 트랜잭션 안의 검사용이라 캐시하지 않는다(커밋 전 변경을 본다).</summary>
    Task<int> CountUsersInGroupsAsync(IReadOnlyCollection<long> userGroupIds, CancellationToken cancellationToken);
}
