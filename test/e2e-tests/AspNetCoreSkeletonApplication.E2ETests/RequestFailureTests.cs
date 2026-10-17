using AspNetCoreSkeletonApplication.E2ETests.Support;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace AspNetCoreSkeletonApplication.E2ETests;

/// <summary>
/// 화면이 요청 실패에 멈추지 않고 알린다 — 처음 불러오는 요청 하나가 실패해도 빈 화면 · 끝없는 "불러오는 중"으로 남지 않는다.
/// 실패는 Playwright 가 해당 요청에 500 으로 대신 답해 만든다. 로그인 화면은 <see cref="LoginPageTests"/>.
/// </summary>
public class RequestFailureTests(E2EFixture fixture) : BrowserTest(fixture)
{
    private const string GenericError = "요청을 처리하지 못했어요";

    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task CurrentUserFailure_IsAnnounced_OnSignedInPages(string spa)
    {
        var page = await OpenAsync(await Fixture.ServerAsync(spa));
        await FailAsync(page, "**/api/auth/me");

        foreach (var path in new[] { "/account", "/admin", "/consent" })
        {
            await page.GotoAsync(path);
            await Expect(Error(page)).ToContainTextAsync(GenericError);
            Assert.EndsWith(path, new Uri(page.Url).AbsolutePath);
        }

        // 상단 바는 로그인 여부를 모르더라도 로그인할 길을 남긴다
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "로그인" })).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task AccountSectionFailures_AreAnnounced(string spa)
    {
        var server = await Fixture.ServerAsync(spa);
        var email = NewEmail(spa, "sections");
        await RegisterAsync(server, email);
        var signedIn = await OpenAsync(server);
        await LogInAsync(signedIn, email, Password);
        await Expect(SignedInAs(signedIn, email)).ToBeVisibleAsync();

        foreach (var request in new[] { "**/api/auth/external/logins", "**/api/account/two-factor", "**/api/account/passkeys" })
        {
            var page = await signedIn.Context.NewPageAsync();
            await FailAsync(page, request);
            await page.GotoAsync("/account");
            await Expect(Error(page)).ToContainTextAsync(GenericError);
            await page.CloseAsync();
        }
    }

    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task LogoutFailure_IsAnnounced_AndKeepsTheSession(string spa)
    {
        var server = await Fixture.ServerAsync(spa);
        var email = NewEmail(spa, "logout");
        await RegisterAsync(server, email);
        var page = await OpenAsync(server);
        await LogInAsync(page, email, Password);
        await Expect(SignedInAs(page, email)).ToBeVisibleAsync();
        await FailAsync(page, "**/api/auth/logout");

        await page.GetByRole(AriaRole.Button, new() { Name = "로그아웃" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("로그아웃하지 못했어요");
        await Expect(SignedInAs(page, email)).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task WeatherFailure_IsAnnounced(string spa)
    {
        var page = await OpenAsync(await Fixture.ServerAsync(spa));
        await FailAsync(page, "**/weatherforecast");

        await page.GotoAsync("/");

        await Expect(Error(page)).ToContainTextAsync("날씨 예보를 불러오지 못했어요");
    }

    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task TwoFactorChallengeFailure_IsNotMistakenForExpiry(string spa)
    {
        var page = await OpenAsync(await Fixture.ServerAsync(spa));
        await FailAsync(page, "**/api/auth/login/2fa");

        await page.GotoAsync("/login?step=two-factor");

        await Expect(Error(page)).ToContainTextAsync(GenericError);
        await Expect(page.Locator("main")).Not.ToContainTextAsync("로그인 시간이 지났어요");
    }

    private static Task FailAsync(IPage page, string url) => page.RouteAsync(url, route => route.FulfillAsync(new() { Status = 500 }));
}
