using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Support;

/// <summary>
/// SQL Server 컨테이너 하나를 테스트 클래스들이 함께 쓴다. 클래스마다 다른 데이터베이스 이름을 써서 서로 섞이지 않게 한다.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    /// <summary>서버에 붙는 기본 연결 문자열(master).</summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>같은 서버의 다른 데이터베이스. 만들어 주지는 않는다 — EnsureCreated · Migrate 가 만든다.</summary>
    public string ConnectionStringFor(string database) =>
        new SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = database }.ConnectionString;

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
