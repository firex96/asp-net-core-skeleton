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

    /// <summary>앱은 인증을 기본으로 요구한다(fallback 정책) — 익명 요청은 라우팅 판정(404 · 405 · 415)보다 먼저 401 이다.</summary>
    public HttpClient CreateAuthenticatedClient(string user = "1")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.HeaderName, user);
        return client;
    }
}
