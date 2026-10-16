using AspNetCoreSkeletonApplication.Common.Persistence;
using AspNetCoreSkeletonApplication.Common.Product.Persistence.Services;
using AspNetCoreSkeletonApplication.FunctionalTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using InternalApiProgram = AspNetCoreSkeletonApplication.InternalApi.Program;

namespace AspNetCoreSkeletonApplication.FunctionalTests.InternalApi;

/// <summary>InternalApi 를 DB 없이 띄운다 — EntityService 와 트랜잭션은 메모리 대역.</summary>
public class InternalApiWebApplicationFactory : WebApplicationFactory<InternalApiProgram>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "Server=unused;Database=unused");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IProductEntityService>();
            services.AddSingleton<IProductEntityService, InMemoryProductEntityService>();
            services.RemoveAll<IProductStatusHistoryEntityService>();
            services.AddSingleton<IProductStatusHistoryEntityService, InMemoryProductStatusHistoryEntityService>();
            services.RemoveAll<IMainTransaction>();
            services.AddSingleton<IMainTransaction, InlineMainTransaction>();
        });
    }
}
