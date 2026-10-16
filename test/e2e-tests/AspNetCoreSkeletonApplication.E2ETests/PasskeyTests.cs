using AspNetCoreSkeletonApplication.E2ETests.Support;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace AspNetCoreSkeletonApplication.E2ETests;

/// <summary>
/// 패스키를 화면으로 — 내 계정에서 등록하고, 로그아웃한 뒤 비밀번호 없이 패스키로 로그인한다. 인증기는 Playwright 의 가상 인증기
/// (<see cref="IBrowserContext.Credentials"/>)라 OS 의 패스키 창은 뜨지 않는다.
/// </summary>
public class PasskeyTests(E2EFixture fixture) : BrowserTest(fixture)
{
    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task RegisteredPasskey_SignsInWithoutAPassword(string spa)
    {
        var server = await Fixture.ServerAsync(spa);
        var email = NewEmail(spa, "passkey");
        await RegisterAsync(server, email);
        var page = await OpenAsync(server);
        await page.Context.Credentials.InstallAsync();
        await LogInAsync(page, email, Password);
        await Expect(SignedInAs(page, email)).ToBeVisibleAsync();

        await page.GotoAsync("/account");
        await page.GetByLabel("패스키 이름").FillAsync("E2E 노트북");
        await page.GetByRole(AriaRole.Button, new() { Name = "패스키 추가" }).ClickAsync();
        await Expect(Notice(page)).ToContainTextAsync("패스키 'E2E 노트북'를 등록했어요");
        await Expect(page.Locator("main").GetByRole(AriaRole.Listitem).Filter(new() { HasText = "E2E 노트북" })).ToBeVisibleAsync();
        Assert.Single(await page.Context.Credentials.GetAsync());

        await page.GetByRole(AriaRole.Button, new() { Name = "로그아웃" }).ClickAsync();
        await Expect(SignedInAs(page, email)).ToBeHiddenAsync();
        await page.GotoAsync("/login");
        await page.GetByRole(AriaRole.Button, new() { Name = "패스키로 로그인" }).ClickAsync();

        await Expect(SignedInAs(page, email)).ToBeVisibleAsync();
    }
}
