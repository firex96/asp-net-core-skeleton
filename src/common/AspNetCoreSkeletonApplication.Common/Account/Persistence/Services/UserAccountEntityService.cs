using AspNetCoreSkeletonApplication.Common.Account.Dtos;
using AspNetCoreSkeletonApplication.Common.Account.Persistence.Mappers;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Account.Persistence.Services;

/// <summary>캐시하지 않는다 — 관리 화면에서만 읽는다.</summary>
internal sealed class UserAccountEntityService(AppDbContext dbContext) : IUserAccountEntityService
{
    public async Task<UserAccountDto> GetAsync(long id, CancellationToken cancellationToken) =>
        await dbContext.Users.Where(user => user.Id == id).ProjectToDto().SingleOrDefaultAsync(cancellationToken)
        ?? throw new ItemNotFoundException($"사용자 {id} 가 없다");

    public async Task<IReadOnlyList<UserAccountDto>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken) =>
        await dbContext.Users.Where(user => ids.Contains(user.Id)).OrderBy(user => user.Id).ProjectToDto().ToListAsync(cancellationToken);

    public async Task<UserAccountDto?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        // Identity 의 UpperInvariantLookupNormalizer 와 같은 정규화
        var normalized = email.Trim().Normalize().ToUpperInvariant();
        return await dbContext.Users.Where(user => user.NormalizedEmail == normalized).ProjectToDto().SingleOrDefaultAsync(cancellationToken);
    }
}
