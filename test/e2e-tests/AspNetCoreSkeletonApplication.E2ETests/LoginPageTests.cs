using AspNetCoreSkeletonApplication.E2ETests.Support;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace AspNetCoreSkeletonApplication.E2ETests;

/// <summary>
/// 로그인 화면이 준비 요청의 실패에 막히지 않는다 — 요청 하나가 실패해도 로그인 폼이 쓸 수 있게 남고, 소셜 로그인 버튼이 말없이 사라지지 않는다.
/// 실패는 Playwright 가 해당 요청에 500 으로 대신 답해 만든다.
/// </summary>
public class LoginPageTests(E2EFixture fixture) : BrowserTest(fixture)
{
    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task CurrentUserFailure_StillOffersSocialLogin(string spa)
    {
        var page = await OpenAsync(await Fixture.ServerAsync(spa));
        await page.RouteAsync("**/api/auth/me", route => route.FulfillAsync(new() { Status = 500 }));

        await page.GotoAsync("/login");

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Google로 계속하기" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "로그인", Exact = true })).ToBeEnabledAsync();
    }

    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task ProvidersFailure_IsAnnounced(string spa)
    {
        var page = await OpenAsync(await Fixture.ServerAsync(spa));
        await page.RouteAsync("**/api/auth/external/providers", route => route.FulfillAsync(new() { Status = 500 }));

        await page.GotoAsync("/login");

        await Expect(Error(page)).ToContainTextAsync("소셜 로그인 목록을 불러오지 못했어요");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "로그인", Exact = true })).ToBeEnabledAsync();
    }
}
