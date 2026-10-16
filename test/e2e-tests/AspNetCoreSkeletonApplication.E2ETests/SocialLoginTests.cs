using AspNetCoreSkeletonApplication.E2ETests.Support;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace AspNetCoreSkeletonApplication.E2ETests;

/// <summary>
/// 소셜 계정 연결 · 로그인 · 끊기를 화면으로 — 버튼은 서버로 가는 진짜 폼 POST 다(SPA 라우터가 가로채면 안 된다). 공급자는 mock OAuth 서버(Google)이고,
/// 같은 공급자 계정을 쓰므로 SPA(DB) 하나에 이 시나리오 하나만 둔다.
/// </summary>
public class SocialLoginTests(E2EFixture fixture) : BrowserTest(fixture)
{
    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task LinkedGoogleAccount_SignsIn_UntilUnlinked(string spa)
    {
        var server = await Fixture.ServerAsync(spa);
        var email = NewEmail(spa, "social");
        await RegisterAsync(server, email);

        var stranger = await OpenAsync(server);
        await SignInWithGoogleAsync(stranger);
        await Expect(Notice(stranger)).ToContainTextAsync("아직 연결하지 않은 Google 계정");

        var owner = await OpenAsync(server);
        await LogInAsync(owner, email, Password);
        await Expect(SignedInAs(owner, email)).ToBeVisibleAsync();
        await owner.GotoAsync("/account");
        await GoogleRow(owner).GetByRole(AriaRole.Button, new() { Name = "연결하기" }).ClickAsync();
        await Expect(Notice(owner)).ToContainTextAsync("Google 계정을 연결했어요");
        await Expect(GoogleRow(owner)).ToContainTextAsync("연결됨");

        var returning = await OpenAsync(server);
        await SignInWithGoogleAsync(returning);
        await Expect(SignedInAs(returning, email)).ToBeVisibleAsync();

        await GoogleRow(owner).GetByRole(AriaRole.Button, new() { Name = "연결 끊기" }).ClickAsync();
        await Expect(GoogleRow(owner)).ToContainTextAsync("연결 안 됨");

        var afterUnlink = await OpenAsync(server);
        await SignInWithGoogleAsync(afterUnlink);
        await Expect(Notice(afterUnlink)).ToContainTextAsync("아직 연결하지 않은 Google 계정");
    }

    private static async Task SignInWithGoogleAsync(IPage page)
    {
        await page.GotoAsync("/login");
        await page.GetByRole(AriaRole.Button, new() { Name = "Google로 계속하기" }).ClickAsync();
    }

    private static ILocator GoogleRow(IPage page) => page.Locator("main").GetByRole(AriaRole.Listitem).Filter(new() { HasText = "Google" });
}
