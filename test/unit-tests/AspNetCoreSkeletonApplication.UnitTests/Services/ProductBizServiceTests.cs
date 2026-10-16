using AspNetCoreSkeletonApplication.Common.Product.Dtos;
using AspNetCoreSkeletonApplication.Common.Product.Enums;
using AspNetCoreSkeletonApplication.Common.Errors;
using AspNetCoreSkeletonApplication.Common.Persistence;
using AspNetCoreSkeletonApplication.Common.Product.Persistence.Services;
using AspNetCoreSkeletonApplication.Common.Product.Services.Biz;
using NSubstitute;

namespace AspNetCoreSkeletonApplication.UnitTests.Services;

/// <summary>BizService 는 I~EntityService 를 대체해 호출 순서와 Domain 판단 적용만 본다 — DB 동작은 통합 테스트가 본다.</summary>
public class ProductBizServiceTests
{
    private readonly IProductEntityService _entityService = Substitute.For<IProductEntityService>();
    private readonly IProductStatusHistoryEntityService _historyService = Substitute.For<IProductStatusHistoryEntityService>();
    private readonly InlineTransaction _transaction = new();

    private ProductBizService CreateBizService() => new(_entityService, _historyService, _transaction);

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
    public async Task GetVisible_SomeoneElsesDraft_IsNotFound_ButAManagerSeesIt()
    {
        _entityService.GetAsync(1, Arg.Any<CancellationToken>()).Returns(Product(1, ProductStatus.Draft));

        await Assert.ThrowsAsync<ItemNotFoundException>(() =>
            CreateBizService().GetVisibleAsync(1, new ProductViewerDto(7, CanManageProducts: false), TestContext.Current.CancellationToken));
        var managed = await CreateBizService().GetVisibleAsync(1, new ProductViewerDto(7, CanManageProducts: true), TestContext.Current.CancellationToken);

        Assert.Equal(1, managed.Id);
    }

    [Fact]
    public async Task List_LeavesOutDraftsTheViewerCannotSee()
    {
        _entityService.ListAsync(Arg.Any<CancellationToken>()).Returns([Product(1, ProductStatus.Draft), Product(2, ProductStatus.OnSale)]);

        var anonymous = await CreateBizService().ListAsync(new ProductViewerDto(null, CanManageProducts: false), TestContext.Current.CancellationToken);

        Assert.Equal([2L], anonymous.Select(product => product.Id));
    }

    [Fact]
    public async Task ChangeStatus_Allowed_UpdatesInsideTransaction()
    {
        _entityService.GetAsync(1, Arg.Any<CancellationToken>()).Returns(Product(1, ProductStatus.Draft));
        _entityService.UpdateStatusAsync(1, ProductStatus.OnSale, Arg.Any<CancellationToken>()).Returns(Product(1, ProductStatus.OnSale));

        var changed = await CreateBizService().ChangeStatusAsync(1, ProductStatus.OnSale, TestContext.Current.CancellationToken);

        Assert.Equal(ProductStatus.OnSale, changed.Status);
        Assert.Equal(1, _transaction.Executions);
        await _historyService.Received(1).CreateAsync(
            new ProductStatusHistoryCreateDto(1, ProductStatus.Draft, ProductStatus.OnSale), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_Disallowed_ThrowsConflictWithoutUpdating()
    {
        _entityService.GetAsync(1, Arg.Any<CancellationToken>()).Returns(Product(1, ProductStatus.Discontinued));

        await Assert.ThrowsAsync<ConflictException>(
            () => CreateBizService().ChangeStatusAsync(1, ProductStatus.OnSale, TestContext.Current.CancellationToken));

        await _entityService.DidNotReceiveWithAnyArgs().UpdateStatusAsync(default, default, TestContext.Current.CancellationToken);
        await _historyService.DidNotReceiveWithAnyArgs().CreateAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_RunsInsideTransaction()
    {
        await CreateBizService().DeleteAsync(1, TestContext.Current.CancellationToken);

        await _entityService.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
        Assert.Equal(1, _transaction.Executions);
    }

    [Fact]
    public async Task DiscontinueStaleDrafts_PagesUntilEmpty_OneTransactionPerPage()
    {
        var now = new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero);
        var cutoff = now.AddDays(-30);
        _entityService.ListStaleDraftsAsync(cutoff, 0, 2, Arg.Any<CancellationToken>())
            .Returns([Product(1, ProductStatus.Draft), Product(2, ProductStatus.Draft)]);
        _entityService.ListStaleDraftsAsync(cutoff, 2, 2, Arg.Any<CancellationToken>())
            .Returns([Product(3, ProductStatus.Draft)]);
        _entityService.ListStaleDraftsAsync(cutoff, 3, 2, Arg.Any<CancellationToken>())
            .Returns([]);

        var processed = await CreateBizService().DiscontinueStaleDraftsAsync(now, 30, 2, TestContext.Current.CancellationToken);

        Assert.Equal(3, processed);
        Assert.Equal(2, _transaction.Executions);
        await _entityService.Received(3).UpdateStatusAsync(Arg.Any<long>(), ProductStatus.Discontinued, Arg.Any<CancellationToken>());
        await _historyService.Received(3).CreateAsync(Arg.Any<ProductStatusHistoryCreateDto>(), Arg.Any<CancellationToken>());
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
