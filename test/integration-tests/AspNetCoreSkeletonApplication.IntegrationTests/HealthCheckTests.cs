using AspNetCoreSkeletonApplication.Infrastructure.EntityFramework.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>SQL Server 컨테이너에 붙은 앱이 DB 연결과 /health 를 통과하는지 확인한다.</summary>
public class HealthCheckTests : IAsyncLifetime
{
    private readonly MsSqlContainer _database = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task AppDbContext_CanConnect()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await dbContext.Database.CanConnectAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Health_IsHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetStringAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal("Healthy", response);
    }
}
