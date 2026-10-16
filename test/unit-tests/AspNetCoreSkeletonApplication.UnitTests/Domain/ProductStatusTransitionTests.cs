using AspNetCoreSkeletonApplication.Common.Product.Domain;
using AspNetCoreSkeletonApplication.Common.Product.Enums;
using AspNetCoreSkeletonApplication.Common.Errors;

namespace AspNetCoreSkeletonApplication.UnitTests.Domain;

/// <summary>Domain 은 순수 함수라 대역 없이 표 하나로 모든 경우를 확인한다.</summary>
public class ProductStatusTransitionTests
{
    [Theory]
    [InlineData(ProductStatus.Draft, ProductStatus.Draft, false)]
    [InlineData(ProductStatus.Draft, ProductStatus.OnSale, true)]
    [InlineData(ProductStatus.Draft, ProductStatus.Discontinued, true)]
    [InlineData(ProductStatus.OnSale, ProductStatus.Draft, false)]
    [InlineData(ProductStatus.OnSale, ProductStatus.OnSale, false)]
    [InlineData(ProductStatus.OnSale, ProductStatus.Discontinued, true)]
    [InlineData(ProductStatus.Discontinued, ProductStatus.Draft, false)]
    [InlineData(ProductStatus.Discontinued, ProductStatus.OnSale, false)]
    [InlineData(ProductStatus.Discontinued, ProductStatus.Discontinued, false)]
    public void CanChange_FollowsTransitionTable(ProductStatus from, ProductStatus to, bool expected)
    {
        Assert.Equal(expected, ProductStatusTransition.CanChange(from, to));
    }

    [Fact]
    public void EnsureCanChange_Disallowed_ThrowsConflict()
    {
        var exception = Assert.Throws<ConflictException>(
            () => ProductStatusTransition.EnsureCanChange(ProductStatus.Discontinued, ProductStatus.OnSale));

        Assert.Equal(409, exception.ErrorCode.Status);
    }

    [Fact]
    public void Initial_IsDraft()
    {
        Assert.Equal(ProductStatus.Draft, ProductStatusTransition.Initial);
    }
}
