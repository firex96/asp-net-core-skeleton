using System.Text.Json.Serialization;

namespace AspNetCoreSkeletonApplication.Common.Product.Enums;

/// <summary>상품 판매 상태. 바꿀 수 있는 방향은 <c>Domain/ProductStatusTransition</c> 이 정한다.</summary>
public enum ProductStatus
{
    [JsonStringEnumMemberName("Draft")]
    Draft,

    [JsonStringEnumMemberName("OnSale")]
    OnSale,

    [JsonStringEnumMemberName("Discontinued")]
    Discontinued,
}
