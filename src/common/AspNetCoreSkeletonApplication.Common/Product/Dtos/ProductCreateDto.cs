namespace AspNetCoreSkeletonApplication.Common.Product.Dtos;

/// <summary>새 상품의 입력값. 상태는 받지 않는다 — 처음 상태는 Domain 이 정한다.</summary>
public sealed record ProductCreateDto(string Name, decimal Price);
