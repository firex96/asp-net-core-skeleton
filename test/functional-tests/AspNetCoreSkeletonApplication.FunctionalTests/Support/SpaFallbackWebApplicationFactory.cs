using Microsoft.AspNetCore.Hosting;

namespace AspNetCoreSkeletonApplication.FunctionalTests.Support;

/// <summary>
/// SPA 빌드 출력(index.html)이 있는 것처럼 띄운다 — 레포에는 wwwroot 가 없어(SPA 는 따로 빌드한다) 폴백이 파일을 못 찾으면
/// 없는 API 경로와 구분되지 않는다. 임시 디렉터리를 web root 로 쓴다.
/// </summary>
public class SpaFallbackWebApplicationFactory : ErrorProbeWebApplicationFactory
{
    private readonly string _webRoot = Directory.CreateTempSubdirectory("spa-fallback-").FullName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), "<!doctype html><title>spa</title>");
        builder.UseWebRoot(_webRoot);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, recursive: true);
        }
    }
}
