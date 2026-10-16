using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AspNetCoreSkeletonApplication.E2ETests.Support;

/// <summary>
/// SPA 하나를 정적 빌드해 그 출력을 웹 루트로 서빙하는 Server — 운영 배포와 같은 같은 출처 구성이다(dev 서버 · 프록시 없음).
/// Kestrel 로 실제 포트에 띄워 브라우저가 붙는다. DB 는 SPA 마다 따로 쓰고, 같은 SPA 의 테스트들은 서버 하나를 함께 쓴다.
/// </summary>
public sealed class SpaServer : IAsyncDisposable
{
    /// <summary>화면 흐름으로 인가 코드를 받는 외부 앱(동의를 묻는다). 콜백 주소는 브라우저가 가로챈다.</summary>
    public const string OAuthClientId = "sample-app";

    public static readonly Uri OAuthCallback = new("https://client.test/callback");

    private readonly WebApplicationFactory<Program> _factory;

    private SpaServer(SpaClient client, Uri origin, WebApplicationFactory<Program> factory)
    {
        Client = client;
        Origin = origin;
        _factory = factory;
    }

    public SpaClient Client { get; }

    /// <summary>브라우저가 여는 출처 — <c>App:PublicOrigin</c>(메일 링크 · OAuth issuer)과 같다.</summary>
    public Uri Origin { get; }

    public IServiceProvider Services => _factory.Services;

    /// <summary>화면 밖에서 API 를 직접 부르는 클라이언트(준비 · 토큰 교환). 인증서는 테스트용 자체 서명이라 검증하지 않는다.</summary>
    public HttpClient CreateApiClient() =>
        new(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        })
        {
            BaseAddress = Origin,
        };

    public static async Task<SpaServer> StartAsync(SpaClient client, InfrastructureFixture infrastructure, X509Certificate2 certificate)
    {
        await BuildAsync(client);

        var database = $"e2e_{client.Name}";
        await infrastructure.RecreateDatabaseAsync(database);

        var port = FreePort();
        var origin = new Uri($"https://localhost:{port}/");
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            infrastructure.ConfigureApp(builder, database);
            builder.UseSetting("App:PublicOrigin", origin.GetLeftPart(UriPartial.Authority));
            builder.UseWebRoot(client.OutputDirectory);

            var prefix = "Authentication:OAuthServer:Clients:0:";
            builder.UseSetting(prefix + "ClientId", OAuthClientId);
            builder.UseSetting(prefix + "DisplayName", "Sample App");
            builder.UseSetting(prefix + "ConsentType", "Explicit");
            builder.UseSetting(prefix + "RedirectUris:0", OAuthCallback.AbsoluteUri);
            builder.UseSetting(prefix + "GrantTypes:0", "authorization_code");
            builder.UseSetting(prefix + "Scopes:0", "openid");
            builder.UseSetting(prefix + "Scopes:1", "email");
        });
        factory.UseKestrel(options => options.ListenLocalhost(port, listen => listen.UseHttps(certificate)));
        factory.StartServer();
        return new SpaServer(client, origin, factory);
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    /// <summary>
    /// <c>npm run build</c> — 테스트를 돌릴 때마다 지금 소스로 빌드한다. 의존성이 없으면 먼저 <c>npm ci</c> 다.
    /// </summary>
    private static async Task BuildAsync(SpaClient client)
    {
        if (!Directory.Exists(Path.Combine(client.Directory, "node_modules")))
        {
            await NpmAsync(client, "ci");
        }

        await NpmAsync(client, "run build");
        if (!File.Exists(Path.Combine(client.OutputDirectory, "index.html")))
        {
            throw new InvalidOperationException($"{client.Name} 빌드 출력({client.OutputDirectory})에 index.html 이 없다");
        }
    }

    private static async Task NpmAsync(SpaClient client, string arguments)
    {
        var start = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("cmd.exe", $"/c npm {arguments}")
            : new ProcessStartInfo("npm", arguments);
        start.WorkingDirectory = client.Directory;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.Environment["NG_CLI_ANALYTICS"] = "false";

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{client.Name}: npm {arguments} 실패({process.ExitCode})\n{await output}\n{await error}");
        }
    }

    /// <summary>
    /// 빈 포트를 미리 고른다 — 출처(<c>App:PublicOrigin</c>)를 앱 기동 전에 구성해야 해서 Kestrel 의 0번 포트를 쓰지 못한다.
    /// </summary>
    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
