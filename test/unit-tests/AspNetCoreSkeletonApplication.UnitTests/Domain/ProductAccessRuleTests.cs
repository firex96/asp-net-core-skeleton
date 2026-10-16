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

    [Theory]
    [InlineData(ProductStatus.Draft, 7L, 7L, false, true)]            // 작성자는 자기 Draft 를 본다
    [InlineData(ProductStatus.Draft, 8L, 7L, false, false)]           // 남의 Draft 는 못 본다
    [InlineData(ProductStatus.Draft, 8L, null, false, false)]         // 익명 · 클라이언트 토큰도 못 본다
    [InlineData(ProductStatus.Draft, 8L, 7L, true, true)]             // 상품 관리자는 본다
    [InlineData(ProductStatus.OnSale, 8L, null, false, true)]         // 판매 중은 누구나
    [InlineData(ProductStatus.Discontinued, 8L, null, false, true)]   // 판매 종료도 누구나
    public void CanView_DraftOnlyForItsCreatorOrManagers(
        ProductStatus status, long createdBy, long? actorId, bool canManage, bool expected)
    {
        Assert.Equal(expected, ProductAccessRule.CanView(status, createdBy, actorId, canManage));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void CanDelete_ForANonUserPrincipal_NeedsManagePermission(bool canManage, bool expected)
    {
        // 사용자가 아닌 주체(클라이언트 토큰)는 자기 상품이 없다 — 시스템(0)이 만든 Draft 라도 권한이 있어야 한다.
        Assert.Equal(expected, ProductAccessRule.CanDelete(ProductStatus.Draft, createdBy: 0, actorId: null, canManage));
    }
}
