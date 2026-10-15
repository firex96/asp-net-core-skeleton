namespace AspNetCoreSkeletonApplication.IntegrationTests.Support;

/// <summary>SQL Server · Redis 컨테이너를 쓰는 테스트 클래스에 <c>[Collection(InfrastructureCollection.Name)]</c> 을 붙인다.</summary>
[CollectionDefinition(Name)]
public sealed class InfrastructureCollection : ICollectionFixture<InfrastructureFixture>
{
    public const string Name = "Infrastructure";
}
