using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AspNetCoreSkeletonApplication.E2ETests.Support;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.Playwright;

[assembly: AssemblyFixture(typeof(E2EFixture))]

namespace AspNetCoreSkeletonApplication.E2ETests.Support;

/// <summary>
/// 어셈블리 전체가 한 벌을 함께 쓴다 — 컨테이너(SQL Server · Redis · Mailpit · mock OAuth), Chromium, SPA 별 서버(처음 쓸 때 빌드해 띄운다).
/// 테스트마다 새 브라우저 컨텍스트(쿠키 없음)를 연다 — <see cref="BrowserTest"/>.
/// <para>
/// 화면을 눈으로 보려면 <c>E2E_HEADED=1</c> 로 돌린다 — Chromium 창을 띄우고 동작마다 <c>E2E_SLOWMO</c> 밀리초(기본 500) 쉰다.
/// 테스트가 병렬로 돌므로 하나만 골라 돌리는 편이 보기 쉽다(README 「테스트」).
/// </para>
/// </summary>
public sealed class E2EFixture : IAsyncLifetime
{
    private readonly InfrastructureFixture _infrastructure = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<SpaServer>>> _servers = new();
    private readonly X509Certificate2 _certificate = LocalhostCertificate();
    private IPlaywright _playwright = null!;

    public IBrowser Browser { get; private set; } = null!;

    public Mailbox Mailbox { get; private set; } = null!;

    /// <summary>SPA 하나의 서버. 처음 부르면 빌드 · 기동하고, 그 뒤로는 같은 서버를 돌려준다.</summary>
    public Task<SpaServer> ServerAsync(string spa) =>
        _servers.GetOrAdd(spa, name => new Lazy<Task<SpaServer>>(() => SpaServer.StartAsync(SpaClient.Named(name), _infrastructure, _certificate))).Value;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_infrastructure.InitializeAsync().AsTask(), Task.Run(InstallChromium));
        Mailbox = new Mailbox(_infrastructure.MailpitApi);
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(LaunchOptions());
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var server in _servers.Values.Where(server => server.IsValueCreated && server.Value.IsCompletedSuccessfully))
        {
            await server.Value.Result.DisposeAsync();
        }

        await Browser.DisposeAsync();
        _playwright.Dispose();
        Mailbox.Dispose();
        await _infrastructure.DisposeAsync();
        _certificate.Dispose();
    }

    private static BrowserTypeLaunchOptions LaunchOptions()
    {
        if (Environment.GetEnvironmentVariable("E2E_HEADED") is not ("1" or "true"))
        {
            return new();
        }

        var slowMo = int.TryParse(Environment.GetEnvironmentVariable("E2E_SLOWMO"), out var milliseconds) ? milliseconds : 500;
        return new() { Headless = false, SlowMo = slowMo };
    }

    /// <summary>Playwright 가 쓰는 Chromium 을 받는다. 이미 있으면 바로 끝난다.</summary>
    private static void InstallChromium()
    {
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Chromium 설치 실패({exitCode})");
        }
    }

    /// <summary>localhost 용 자체 서명 인증서 — 개발 인증서가 없는 환경(CI)에서도 HTTPS(<c>__Host-</c> 쿠키 · Secure)로 띄운다.</summary>
    private static X509Certificate2 LocalhostCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("localhost");
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], critical: false));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(7));
        // 메모리 키 그대로는 플랫폼에 따라 TLS 에 못 쓴다 — PFX 로 한 번 내보냈다 다시 읽는다.
        return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), password: null);
    }
}
