using System.Net.Http.Json;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace AspNetCoreSkeletonApplication.E2ETests.Support;

/// <summary>
/// 브라우저 테스트의 바탕 — 테스트마다 새 컨텍스트(쿠키 없음)를 열고, 실패하면 Playwright 트레이스를 <c>bin/…/playwright-traces</c> 에 남긴다
/// (<c>npx playwright show-trace &lt;파일&gt;</c> 로 본다). 세 SPA 는 같은 문구 · 같은 접근성 구조라 선택자를 함께 쓴다.
/// </summary>
public abstract class BrowserTest(E2EFixture fixture) : IAsyncDisposable
{
    public const string Password = "Passw0rd!-long";
    public const string NewPassword = "N3w-Passw0rd!-long";

    private readonly List<IBrowserContext> _contexts = [];

    protected E2EFixture Fixture => fixture;

    /// <summary>쿠키 없는 새 브라우저로 <paramref name="server"/> 를 연다. 한 테스트 안에서 여러 번 열면 서로 다른 사람의 브라우저다.</summary>
    protected async Task<IPage> OpenAsync(SpaServer server)
    {
        var context = await fixture.Browser.NewContextAsync(new()
        {
            BaseURL = server.Origin.ToString(),
            IgnoreHTTPSErrors = true,
            Locale = "ko-KR",
        });
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        _contexts.Add(context);
        return await context.NewPageAsync();
    }

    /// <summary>테스트끼리 서버(DB)를 함께 쓰므로 이메일이 겹치지 않게 만든다.</summary>
    protected static string NewEmail(string spa, string purpose) => $"{purpose}-{spa}-{Guid.NewGuid():N}"[..40] + "@example.com";

    /// <summary>화면을 거치지 않고 가입하고 이메일도 확인해 둔다 — 가입 · 확인 화면이 시험 대상이 아닐 때.</summary>
    protected static async Task RegisterAsync(SpaServer server, string email)
    {
        using var api = server.CreateApiClient();
        (await api.PostAsJsonAsync("/api/auth/register", new { email, password = Password }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        using var scope = server.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email) ?? throw new InvalidOperationException($"{email} 가 없다");
        if (!(await users.ConfirmEmailAsync(user, await users.GenerateEmailConfirmationTokenAsync(user))).Succeeded)
        {
            throw new InvalidOperationException($"{email} 를 확인하지 못했다");
        }
    }

    protected static async Task LogInAsync(IPage page, string email, string password)
    {
        await page.GotoAsync("/login");
        await page.GetByLabel("이메일").FillAsync(email);
        await page.GetByLabel("비밀번호", new() { Exact = true }).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "로그인", Exact = true }).ClickAsync();
    }

    /// <summary>상단 바의 내 계정 링크 — 로그인돼 있으면 이메일이 보인다.</summary>
    protected static ILocator SignedInAs(IPage page, string email) => page.GetByRole(AriaRole.Link, new() { Name = email });

    /// <summary>본문의 안내(role=status). 본문으로 한정한다 — Nuxt 는 경로 안내용 status 를 따로 둔다.</summary>
    protected static ILocator Notice(IPage page) => page.Locator("main").GetByRole(AriaRole.Status);

    /// <summary>본문의 오류(role=alert).</summary>
    protected static ILocator Error(IPage page) => page.Locator("main").GetByRole(AriaRole.Alert);

    public async ValueTask DisposeAsync()
    {
        var test = TestContext.Current;
        var failed = test.TestState?.Result == TestResult.Failed;
        for (var index = 0; index < _contexts.Count; index++)
        {
            var path = failed ? Path.Combine(AppContext.BaseDirectory, "playwright-traces", $"{FileName(test.Test?.TestDisplayName)}-{index}.zip") : null;
            await _contexts[index].Tracing.StopAsync(new() { Path = path });
            await _contexts[index].CloseAsync();
        }

        GC.SuppressFinalize(this);
    }

    private static string FileName(string? name) =>
        string.Concat((name ?? "test").Select(character => Path.GetInvalidFileNameChars().Contains(character) || character == ' ' ? '_' : character));
}
