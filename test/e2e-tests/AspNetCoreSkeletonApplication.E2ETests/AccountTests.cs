using AspNetCoreSkeletonApplication.E2ETests.Support;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace AspNetCoreSkeletonApplication.E2ETests;

/// <summary>가입 · 로그인 · 로그아웃 · 잠금 · 비밀번호 재설정을 SPA 화면으로 끝까지 — SPA 마다 같은 시나리오다.</summary>
public class AccountTests(E2EFixture fixture) : BrowserTest(fixture)
{
    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task SignUp_ExplainsAWeakPassword_ThenSignsInAndOut(string spa)
    {
        var page = await OpenAsync(await Fixture.ServerAsync(spa));
        var email = NewEmail(spa, "sign-up");

        await page.GotoAsync("/login");
        await page.GetByRole(AriaRole.Button, new() { Name = "회원가입", Exact = true }).ClickAsync();
        await page.GetByLabel("이메일").FillAsync(email);
        await page.GetByLabel("비밀번호", new() { Exact = true }).FillAsync("short");
        await page.GetByLabel("비밀번호 확인").FillAsync("short");
        await page.GetByRole(AriaRole.Button, new() { Name = "가입하기" }).ClickAsync();

        await Expect(Error(page)).ToContainTextAsync("입력한 내용을 확인해 주세요.");
        await Expect(Error(page).GetByRole(AriaRole.Listitem).First).ToBeVisibleAsync();

        await page.GetByLabel("비밀번호", new() { Exact = true }).FillAsync(Password);
        await page.GetByLabel("비밀번호 확인").FillAsync(Password);
        await page.GetByRole(AriaRole.Button, new() { Name = "가입하기" }).ClickAsync();

        await Expect(SignedInAs(page, email)).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "로그아웃" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "로그인", Exact = true })).ToBeVisibleAsync();
        await Expect(SignedInAs(page, email)).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task FiveWrongPasswords_LockTheAccount(string spa)
    {
        var server = await Fixture.ServerAsync(spa);
        var email = NewEmail(spa, "lockout");
        await RegisterAsync(server, email);
        var page = await OpenAsync(server);

        await page.GotoAsync("/login");
        await page.GetByLabel("이메일").FillAsync(email);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await SubmitPasswordAsync(page, "Wr0ng-Passw0rd!");
        }

        await SubmitPasswordAsync(page, Password);

        await Expect(Error(page)).ToContainTextAsync("계정이 잠겼어요");
        await Expect(SignedInAs(page, email)).ToBeHiddenAsync();
    }

    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task ForgotPassword_TheMailedLinkSetsANewPassword(string spa)
    {
        var server = await Fixture.ServerAsync(spa);
        var email = NewEmail(spa, "reset");
        await RegisterAsync(server, email);
        var page = await OpenAsync(server);

        await page.GotoAsync("/login");
        await page.GetByRole(AriaRole.Link, new() { Name = "비밀번호를 잊으셨나요?" }).ClickAsync();
        // 화면 코드를 늦게 불러오는 SPA(SvelteKit · Nuxt)는 잠깐 이전 화면이 남는다 — 새 화면이 뜬 뒤에 입력한다.
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "비밀번호 찾기" })).ToBeVisibleAsync();
        await page.GetByLabel("이메일").FillAsync(email);
        await page.GetByRole(AriaRole.Button, new() { Name = "재설정 링크 받기" }).ClickAsync();
        await Expect(Notice(page)).ToBeVisibleAsync();

        var link = await Fixture.Mailbox.WaitForLinkAsync(email);
        Assert.Equal(new Uri(server.Origin, "/reset-password"), new Uri(link.GetLeftPart(UriPartial.Path)));
        await page.GotoAsync(link.ToString());
        await page.GetByLabel("새 비밀번호", new() { Exact = true }).FillAsync(NewPassword);
        await page.GetByLabel("새 비밀번호 확인").FillAsync(NewPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "비밀번호 바꾸기" }).ClickAsync();

        await Expect(Notice(page)).ToContainTextAsync("비밀번호를 바꿨어요");

        await LogInAsync(page, email, NewPassword);

        await Expect(SignedInAs(page, email)).ToBeVisibleAsync();
    }

    /// <summary>비밀번호를 넣고 로그인을 누른 뒤 서버 응답까지 기다린다 — 같은 오류 문구가 이어져도 시도가 하나씩 끝나게.</summary>
    private static async Task SubmitPasswordAsync(IPage page, string password)
    {
        await page.GetByLabel("비밀번호", new() { Exact = true }).FillAsync(password);
        await page.RunAndWaitForResponseAsync(
            () => page.GetByRole(AriaRole.Button, new() { Name = "로그인", Exact = true }).ClickAsync(),
            "**/api/auth/login");
    }
}
