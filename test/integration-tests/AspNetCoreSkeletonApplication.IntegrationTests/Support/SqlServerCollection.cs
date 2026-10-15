namespace AspNetCoreSkeletonApplication.IntegrationTests.Support;

/// <summary>SQL Server 컨테이너를 쓰는 테스트 클래스에 <c>[Collection(SqlServerCollection.Name)]</c> 을 붙인다.</summary>
[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}
