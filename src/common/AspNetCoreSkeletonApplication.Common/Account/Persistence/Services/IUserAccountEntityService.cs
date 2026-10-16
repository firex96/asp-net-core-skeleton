using AspNetCoreSkeletonApplication.Common.Account.Dtos;

namespace AspNetCoreSkeletonApplication.Common.Account.Persistence.Services;

/// <summary>로그인 계정(Identity 의 <c>identity_user</c>) 조회. 가입 · 비밀번호 같은 변경은 Identity 의 UserManager 가 맡는다.</summary>
public interface IUserAccountEntityService
{
    /// <summary>없으면 <c>ItemNotFoundException</c>(404).</summary>
    Task<UserAccountDto> GetAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserAccountDto>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken);

    /// <summary>이메일이 정확히 같은 계정(대소문자는 가리지 않는다 — Identity 의 정규화 이메일로 찾는다). 없으면 null.</summary>
    Task<UserAccountDto?> FindByEmailAsync(string email, CancellationToken cancellationToken);
}
