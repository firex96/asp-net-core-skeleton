using AspNetCoreSkeletonApplication.E2ETests.Support;
using AspNetCoreSkeletonApplication.IntegrationTests.Support;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace AspNetCoreSkeletonApplication.E2ETests;

/// <summary>
/// 2단계 인증을 화면으로 — 내 계정에서 켜고(인증 앱은 화면의 키로 코드를 계산, 메일 코드는 Mailpit 에서 읽는다), 다음 로그인에서 두 번째 단계를 거친다.
/// </summary>
public class TwoFactorTests(E2EFixture fixture) : BrowserTest(fixture)
{
    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task Authenticator_EnabledOnTheAccountPage_IsAskedAtTheNextLogin(string spa)
    {
        var server = await Fixture.ServerAsync(spa);
        var email = NewEmail(spa, "authenticator");
        await RegisterAsync(server, email);
        var page = await OpenAsync(server);
        await LogInAsync(page, email, Password);
        await Expect(SignedInAs(page, email)).ToBeVisibleAsync();

        await page.GotoAsync("/account");
        await page.GetByRole(AriaRole.Button, new() { Name = "인증 앱으로 켜기" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Img, new() { Name = "인증 앱 등록 QR 코드" })).ToBeVisibleAsync();
        var key = await page.GetByTestId("shared-key").InnerTextAsync();
        await page.GetByLabel("인증 앱의 6자리 코드").FillAsync(Totp.Code(key));
        await page.GetByRole(AriaRole.Button, new() { Name = "확인하고 켜기" }).ClickAsync();
        await Expect(Notice(page)).ToContainTextAsync("복구 코드예요");
        await Expect(page.Locator("main")).ToContainTextAsync("켜져 있어요 — 인증 앱");

        await page.GetByRole(AriaRole.Button, new() { Name = "로그아웃" }).ClickAsync();
        await LogInAsync(page, email, Password);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "2단계 인증" })).ToBeVisibleAsync();
        await page.GetByLabel("확인 코드").FillAsync("000000");
        await page.GetByRole(AriaRole.Button, new() { Name = "확인", Exact = true }).ClickAsync();
        await Expect(Error(page)).ToContainTextAsync("코드가 맞지 않아요");
        await page.GetByLabel("확인 코드").FillAsync(Totp.Code(key));
        await page.GetByRole(AriaRole.Button, new() { Name = "확인", Exact = true }).ClickAsync();

        await Expect(SignedInAs(page, email)).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task EmailCode_IsMailedAtTheNextLogin(string spa)
    {
        var server = await Fixture.ServerAsync(spa);
        var email = NewEmail(spa, "email-code");
        await RegisterAsync(server, email);
        var page = await OpenAsync(server);
        await LogInAsync(page, email, Password);
        await Expect(SignedInAs(page, email)).ToBeVisibleAsync();

        await page.GotoAsync("/account");
        await page.GetByRole(AriaRole.Button, new() { Name = "메일 코드로 켜기" }).ClickAsync();
        await Expect(page.Locator("main")).ToContainTextAsync("켜져 있어요 — 메일 코드");

        await page.GetByRole(AriaRole.Button, new() { Name = "로그아웃" }).ClickAsync();
        await LogInAsync(page, email, Password);
        await Expect(Notice(page)).ToContainTextAsync("메일로 확인 코드를 보냈어요");
        await page.GetByLabel("확인 코드").FillAsync(await Fixture.Mailbox.WaitForSignInCodeAsync(email));
        await page.GetByRole(AriaRole.Button, new() { Name = "확인", Exact = true }).ClickAsync();

        await Expect(SignedInAs(page, email)).ToBeVisibleAsync();
    }
}
