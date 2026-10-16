using AspNetCoreSkeletonApplication.Common.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Support;

/// <summary>
/// SQL Server · Redis · Mailpit · mock OAuth 컨테이너 한 벌을 테스트 클래스들이 함께 쓴다. 클래스마다 다른 데이터베이스 이름과 캐시 키 접두사를 써서
/// 서로 섞이지 않게 한다 — 앱을 띄울 때는 <see cref="ConfigureApp"/> 하나로 맞춘다.
/// </summary>
public sealed class InfrastructureFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7.4-alpine").Build();

    // 메일 수신함 — 공식 Testcontainers 모듈이 없어 일반 컨테이너로 띄운다. 앱은 언제나 SMTP 인증을 하므로 아무 계정이나 받게 한다.
    private readonly IContainer _mailpit = new ContainerBuilder("axllent/mailpit:v1.31.6")
        .WithEnvironment("MP_SMTP_AUTH_ACCEPT_ANY", "1")
        .WithEnvironment("MP_SMTP_AUTH_ALLOW_INSECURE", "1")
        .WithPortBinding(MailpitSmtpPort, assignRandomHostPort: true)
        .WithPortBinding(MailpitApiPort, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(MailpitApiPort).ForPath("/livez")))
        .Build();

    // 소셜 로그인 공급자 흉내 — Google · Kakao · Naver 를 issuer 하나씩. 설정은 docker compose 와 같은 파일이다.
    private readonly IContainer _mockOAuth = new ContainerBuilder("ghcr.io/navikt/mock-oauth2-server:6.0.8")
        .WithEnvironment("JSON_CONFIG", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "mock-oauth2-server.json")))
        .WithPortBinding(MockOAuthPort, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(MockOAuthPort).ForPath("/google/.well-known/openid-configuration")))
        .Build();

    private const ushort MockOAuthPort = 8080;
    private const ushort MailpitSmtpPort = 1025;
    private const ushort MailpitApiPort = 8025;

    /// <summary>앱이 메일 링크에 쓰는 SPA 출처(테스트용 가짜 주소).</summary>
    public const string PublicOrigin = "https://spa.test";

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _generations = new();

    /// <summary>SQL Server 에 붙는 기본 연결 문자열(master).</summary>
    public string ConnectionString => _sqlServer.GetConnectionString();

    public string RedisConnectionString => _redis.GetConnectionString();

    /// <summary>Mailpit REST API(<c>/api/v1/…</c>) 주소 — 받은 메일을 읽는다.</summary>
    public Uri MailpitApi => new($"http://{_mailpit.Hostname}:{_mailpit.GetMappedPublicPort(MailpitApiPort)}/");

    /// <summary>같은 서버의 다른 데이터베이스. 만들어 주지는 않는다 — EnsureCreated · Migrate 가 만든다.</summary>
    public string ConnectionStringFor(string database) =>
        new SqlConnectionStringBuilder(_sqlServer.GetConnectionString()) { InitialCatalog = database }.ConnectionString;

    /// <summary>앱을 Testing 환경으로, <paramref name="database"/> 데이터베이스와 그 이름을 접두사로 쓰는 캐시에 붙인다.</summary>
    public void ConfigureApp(IWebHostBuilder builder, string database)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", ConnectionStringFor(database));
        builder.UseSetting("ConnectionStrings:Redis", RedisConnectionString);
        // DB 를 새로 만들 때마다 캐시 접두사도 바꾼다 — 같은 ID(1, 2 …)의 옛 행이 캐시에 남아 새 DB 의 행처럼 보이지 않게.
        builder.UseSetting("Cache:KeyPrefix", $"{database}:{_generations.GetValueOrDefault(database, "0")}:");
        builder.UseSetting("App:PublicOrigin", PublicOrigin);
        builder.UseSetting("Mail:Smtp:Host", _mailpit.Hostname);
        builder.UseSetting("Mail:Smtp:Port", _mailpit.GetMappedPublicPort(MailpitSmtpPort).ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Mail:Smtp:Security", "None");
        builder.UseSetting("Mail:Smtp:Username", "test");
        builder.UseSetting("Mail:Smtp:Password", "test");
        builder.UseSetting("Mail:Smtp:From", "no-reply@spa.test");
        builder.UseSetting("Authentication:OAuthServer:KeySource", "Ephemeral");
        foreach (var provider in new[] { "Google", "Kakao", "Naver" })
        {
            var issuer = $"http://{_mockOAuth.Hostname}:{_mockOAuth.GetMappedPublicPort(MockOAuthPort)}/{provider.ToLowerInvariant()}";
            builder.UseSetting($"Authentication:External:{provider}:ClientId", "test");
            builder.UseSetting($"Authentication:External:{provider}:ClientSecret", "test");
            builder.UseSetting($"Authentication:External:{provider}:AuthorizationEndpoint", $"{issuer}/authorize");
            builder.UseSetting($"Authentication:External:{provider}:TokenEndpoint", $"{issuer}/token");
            builder.UseSetting($"Authentication:External:{provider}:UserInformationEndpoint", $"{issuer}/userinfo");
            builder.UseSetting($"Authentication:External:{provider}:Scope:0", "openid"); // mock 서버가 요구한다
        }
    }

    /// <summary>
    /// <paramref name="database"/> 를 지우고 마이그레이션을 처음부터 적용한다. 기동하면서 바로 DB 를 쓰는 앱(Batch 의 Quartz)은
    /// 앱을 띄우기 전에 이것을 부른다.
    /// </summary>
    public async Task RecreateDatabaseAsync(string database)
    {
        await using var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionStringFor(database))
            .Options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
        _generations[database] = Guid.NewGuid().ToString("N")[..8];
    }

    public async ValueTask InitializeAsync() =>
        await Task.WhenAll(_sqlServer.StartAsync(), _redis.StartAsync(), _mailpit.StartAsync(), _mockOAuth.StartAsync());

    public async ValueTask DisposeAsync()
    {
        await _mockOAuth.DisposeAsync();
        await _mailpit.DisposeAsync();
        await _redis.DisposeAsync();
        await _sqlServer.DisposeAsync();
    }
}
