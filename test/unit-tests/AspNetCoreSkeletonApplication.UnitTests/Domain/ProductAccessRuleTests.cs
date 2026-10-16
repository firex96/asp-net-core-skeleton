using AspNetCoreSkeletonApplication.Common.Product.Domain;
using AspNetCoreSkeletonApplication.Common.Product.Enums;

namespace AspNetCoreSkeletonApplication.UnitTests.Domain;

public class ProductAccessRuleTests
{
    [Theory]
    [InlineData(ProductStatus.Draft, 7, 7, false, true)]          // 자기 Draft
    [InlineData(ProductStatus.Draft, 8, 7, false, false)]         // 남의 Draft
    [InlineData(ProductStatus.OnSale, 7, 7, false, false)]        // 자기 것이라도 판매 시작 뒤에는 안 된다
    [InlineData(ProductStatus.OnSale, 8, 7, true, true)]          // 관리 권한이면 누구 것이든
    [InlineData(ProductStatus.Discontinued, 8, 7, true, true)]
    public void CanDelete_OwnDraftOrWithManagePermission(
        ProductStatus status, long createdBy, long actorId, bool canManage, bool expected)
    {
        Assert.Equal(expected, ProductAccessRule.CanDelete(status, createdBy, actorId, canManage));
    }
}
