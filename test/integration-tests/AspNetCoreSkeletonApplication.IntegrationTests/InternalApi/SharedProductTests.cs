using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using InternalApiProgram = AspNetCoreSkeletonApplication.InternalApi.Program;

namespace AspNetCoreSkeletonApplication.IntegrationTests.InternalApi;

/// <summary>두 앱이 Common 을 함께 쓴다 — Server 가 만든 상품을 InternalApi 가 같은 DB 에서 읽는다.</summary>
[Collection(InfrastructureCollection.Name)]
public class SharedProductTests(InfrastructureFixture database) : IAsyncLifetime
{
    private WebApplicationFactory<Program> _server = null!;
    private WebApplicationFactory<InternalApiProgram> _internalApi = null!;

    public async ValueTask InitializeAsync()
    {
        _server = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            database.ConfigureApp(builder, "shared_product");
        });
        _internalApi = new WebApplicationFactory<InternalApiProgram>().WithWebHostBuilder(builder =>
        {
            database.ConfigureApp(builder, "shared_product");
        });

        using var scope = _server.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _internalApi.DisposeAsync();
        await _server.DisposeAsync();
    }

    [Fact]
    public async Task ProductCreatedByServer_IsReadByInternalApi()
    {
        var created = await ReadJsonAsync(await _server.CreateClient().PostAsJsonAsync(
            "/api/products", new { name = "Monitor", price = 320000 }, TestContext.Current.CancellationToken));
        var id = created.GetProperty("id").GetInt64();

        var fetched = await ReadJsonAsync(await _internalApi.CreateClient().GetAsync(
            $"/api/products/{id}", TestContext.Current.CancellationToken));

        Assert.Equal("Monitor", fetched.GetProperty("name").GetString());
        Assert.Equal(0, fetched.GetProperty("createdBy").GetInt64());
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
    }
}
