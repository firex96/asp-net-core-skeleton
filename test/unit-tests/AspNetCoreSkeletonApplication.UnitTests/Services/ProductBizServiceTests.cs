using AspNetCoreSkeletonApplication.Common.Dtos;
using AspNetCoreSkeletonApplication.Common.Enums;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence;
using AspNetCoreSkeletonApplication.Common.Persistence.Services;
using AspNetCoreSkeletonApplication.Common.Services.Biz;
using NSubstitute;

namespace AspNetCoreSkeletonApplication.UnitTests.Services;

/// <summary>BizService 는 I~EntityService 를 대체해 호출 순서와 Domain 판단 적용만 본다 — DB 동작은 통합 테스트가 본다.</summary>
public class ProductBizServiceTests
{
    private readonly IProductEntityService _entityService = Substitute.For<IProductEntityService>();
    private readonly InlineTransaction _transaction = new();

    private ProductBizService CreateBizService() => new(_entityService, _transaction);

    [Fact]
    public async Task Create_StartsAsInitialStatusInsideTransaction()
    {
        var input = new ProductCreateDto("Keyboard", 49_000m);
        _entityService.CreateAsync(input, ProductStatus.Draft, Arg.Any<CancellationToken>()).Returns(Product(1, ProductStatus.Draft));

        var created = await CreateBizService().CreateAsync(input, TestContext.Current.CancellationToken);

        Assert.Equal(ProductStatus.Draft, created.Status);
        Assert.Equal(1, _transaction.Executions);
    }

    [Fact]
    public async Task ChangeStatus_Allowed_UpdatesInsideTransaction()
    {
        _entityService.GetAsync(1, Arg.Any<CancellationToken>()).Returns(Product(1, ProductStatus.Draft));
        _entityService.UpdateStatusAsync(1, ProductStatus.OnSale, Arg.Any<CancellationToken>()).Returns(Product(1, ProductStatus.OnSale));

        var changed = await CreateBizService().ChangeStatusAsync(1, ProductStatus.OnSale, TestContext.Current.CancellationToken);

        Assert.Equal(ProductStatus.OnSale, changed.Status);
        Assert.Equal(1, _transaction.Executions);
    }

    [Fact]
    public async Task ChangeStatus_Disallowed_ThrowsConflictWithoutUpdating()
    {
        _entityService.GetAsync(1, Arg.Any<CancellationToken>()).Returns(Product(1, ProductStatus.Discontinued));

        await Assert.ThrowsAsync<ConflictException>(
            () => CreateBizService().ChangeStatusAsync(1, ProductStatus.OnSale, TestContext.Current.CancellationToken));

        await _entityService.DidNotReceiveWithAnyArgs().UpdateStatusAsync(default, default, TestContext.Current.CancellationToken);
    }

    private static ProductDto Product(long id, ProductStatus status) =>
        new(id, "Keyboard", 49_000m, status, 0, DateTimeOffset.UnixEpoch, 0, DateTimeOffset.UnixEpoch);

    /// <summary>트랜잭션 없이 바로 실행하고 몇 번 불렸는지만 센다.</summary>
    private sealed class InlineTransaction : IMainTransaction
    {
        public int Executions { get; private set; }

        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {
            Executions++;
            return action(cancellationToken);
        }
    }
}
