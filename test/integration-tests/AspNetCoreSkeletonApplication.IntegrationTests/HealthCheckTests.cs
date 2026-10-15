using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.IntegrationTests;

/// <summary>SQL Server 컨테이너에 붙은 앱이 DB 연결과 /health 를 통과하는지 확인한다.</summary>
[Collection(SqlServerCollection.Name)]
public class HealthCheckTests(SqlServerFixture database) : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;

    public ValueTask InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", database.ConnectionString);
        });
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

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
