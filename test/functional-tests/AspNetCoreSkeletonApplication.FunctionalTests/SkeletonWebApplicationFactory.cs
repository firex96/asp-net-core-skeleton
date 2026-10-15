using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AspNetCoreSkeletonApplication.FunctionalTests;

/// <summary>
/// 비밀값 출처 없이(Testing 환경) 앱을 띄운다. DB 에는 붙지 않으므로 연결 문자열은 형식만 맞춘 값이다.
/// </summary>
public class SkeletonWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "Server=unused;Database=unused");
        builder.UseSetting("OpenApi:Enabled", "true");
    }
}
