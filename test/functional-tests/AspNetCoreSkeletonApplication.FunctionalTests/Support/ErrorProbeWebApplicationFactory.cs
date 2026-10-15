using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Support;

/// <summary>앱에 <see cref="ErrorProbeController"/> 를 더해 띄운다.</summary>
public class ErrorProbeWebApplicationFactory : SkeletonWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ErrorProbeController).Assembly));
    }
}
