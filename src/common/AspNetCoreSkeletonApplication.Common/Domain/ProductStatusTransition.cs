using AspNetCoreSkeletonApplication.Common.Enums;
using AspNetCoreSkeletonApplication.Common.Errors;

namespace AspNetCoreSkeletonApplication.Common.Domain;

/// <summary>
/// 상품 상태 전이 규칙. EF 도 DI 도 모르는 순수 함수다 — BizService 는 이것을 불러 판단을 얻기만 한다.
/// <code>
/// Draft ──▶ OnSale ──▶ Discontinued
///   └──────────────────────▲
/// </code>
/// 판매 종료(Discontinued)는 끝 상태다. 같은 상태로의 전이도 받지 않는다.
/// </summary>
public static class ProductStatusTransition
{
    /// <summary>새 상품의 처음 상태.</summary>
    public const ProductStatus Initial = ProductStatus.Draft;

    private static readonly Dictionary<ProductStatus, ProductStatus[]> Allowed = new()
    {
        [ProductStatus.Draft] = [ProductStatus.OnSale, ProductStatus.Discontinued],
        [ProductStatus.OnSale] = [ProductStatus.Discontinued],
        [ProductStatus.Discontinued] = [],
    };

    public static bool CanChange(ProductStatus from, ProductStatus to) => Allowed[from].Contains(to);

    /// <summary>허용되지 않으면 <see cref="ConflictException"/>(409).</summary>
    public static void EnsureCanChange(ProductStatus from, ProductStatus to)
    {
        if (!CanChange(from, to))
        {
            throw new ConflictException($"상품 상태를 {from} 에서 {to} 로 바꿀 수 없다");
        }
    }
}
