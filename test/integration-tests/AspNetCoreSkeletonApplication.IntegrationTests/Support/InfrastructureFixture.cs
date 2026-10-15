using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Support;

/// <summary>
/// SQL Server · Redis 컨테이너 한 벌을 테스트 클래스들이 함께 쓴다. 클래스마다 다른 데이터베이스 이름과 캐시 키 접두사를 써서
/// 서로 섞이지 않게 한다 — 앱을 띄울 때는 <see cref="ConfigureApp"/> 하나로 맞춘다.
/// </summary>
public sealed class InfrastructureFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7.4-alpine").Build();

    /// <summary>SQL Server 에 붙는 기본 연결 문자열(master).</summary>
    public string ConnectionString => _sqlServer.GetConnectionString();

    public string RedisConnectionString => _redis.GetConnectionString();

    /// <summary>같은 서버의 다른 데이터베이스. 만들어 주지는 않는다 — EnsureCreated · Migrate 가 만든다.</summary>
    public string ConnectionStringFor(string database) =>
        new SqlConnectionStringBuilder(_sqlServer.GetConnectionString()) { InitialCatalog = database }.ConnectionString;

    /// <summary>앱을 Testing 환경으로, <paramref name="database"/> 데이터베이스와 그 이름을 접두사로 쓰는 캐시에 붙인다.</summary>
    public void ConfigureApp(IWebHostBuilder builder, string database)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", ConnectionStringFor(database));
        builder.UseSetting("ConnectionStrings:Redis", RedisConnectionString);
        builder.UseSetting("Cache:KeyPrefix", $"{database}:");
    }

    /// <summary>
    /// <paramref name="database"/> 를 지우고 마이그레이션을 처음부터 적용한다. 기동하면서 바로 DB 를 쓰는 앱(Batch 의 Quartz)은
    /// 앱을 띄우기 전에 이것을 부른다.
    /// </summary>
    public async Task RecreateDatabaseAsync(string database)
    {
        await using var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionStringFor(database))
            .Options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async ValueTask InitializeAsync() => await Task.WhenAll(_sqlServer.StartAsync(), _redis.StartAsync());

    public async ValueTask DisposeAsync()
    {
        await _redis.DisposeAsync();
        await _sqlServer.DisposeAsync();
    }
}
