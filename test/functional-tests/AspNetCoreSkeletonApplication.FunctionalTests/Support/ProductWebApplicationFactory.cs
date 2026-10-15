using AspNetCoreSkeletonApplication.Common.Persistence;
using AspNetCoreSkeletonApplication.Common.Persistence.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Support;

/// <summary>EntityService 와 트랜잭션을 메모리 대역으로 바꿔 DB 없이 앱을 띄운다.</summary>
public class ProductWebApplicationFactory : SkeletonWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
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
