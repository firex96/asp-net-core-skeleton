using AspNetCoreSkeletonApplication.Common.Dtos;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.Common.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreSkeletonApplication.Common.Persistence.Services;

internal sealed class ProductStatusHistoryEntityService(AppDbContext dbContext) : IProductStatusHistoryEntityService
{
    public async Task<ProductStatusHistoryDto> CreateAsync(ProductStatusHistoryCreateDto history, CancellationToken cancellationToken)
    {
        var entity = history.ToEntity();
        dbContext.ProductStatusHistories.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<IReadOnlyList<ProductStatusHistoryDto>> ListByProductAsync(long productId, CancellationToken cancellationToken) =>
        await dbContext.ProductStatusHistories
            .Where(history => history.ProductId == productId)
            .OrderBy(history => history.Id)
            .ProjectToDto()
            .ToListAsync(cancellationToken);
}
