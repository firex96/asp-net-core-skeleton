using AspNetCoreSkeletonApplication.Common.Account.Dtos;

namespace AspNetCoreSkeletonApplication.Common.Account.Persistence.Services;

/// <summary>로그인 계정(Identity 의 <c>identity_user</c>) 조회. 가입 · 비밀번호 같은 변경은 Identity 의 UserManager 가 맡는다.</summary>
public interface IUserAccountEntityService
{
    /// <summary>없으면 <c>ItemNotFoundException</c>(404).</summary>
    Task<UserAccountDto> GetAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserAccountDto>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken);
}
