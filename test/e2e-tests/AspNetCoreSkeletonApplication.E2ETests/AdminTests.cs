using AspNetCoreSkeletonApplication.E2ETests.Support;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace AspNetCoreSkeletonApplication.E2ETests;

/// <summary>
/// 관리 화면을 끝까지 — 관리자가 그룹을 만들어 다른 사람을 넣고, 권한을 가진 역할을 만들어 그룹에 주면 그 사람의 권한 목록에 나타난다.
/// 권한이 없는 사람에게는 "관리" 링크가 보이지 않는다.
/// </summary>
public class AdminTests(E2EFixture fixture) : BrowserTest(fixture)
{
    [Theory]
    [MemberData(nameof(SpaClient.Names), MemberType = typeof(SpaClient))]
    public async Task GroupRoleAndPermission_ReachTheMember(string spa)
    {
        var server = await Fixture.ServerAsync(spa);
        var adminEmail = NewEmail(spa, "admin");
        var memberEmail = NewEmail(spa, "editor");
        await RegisterAsync(server, adminEmail);
        await RegisterAsync(server, memberEmail);
        await GrantAdministratorAsync(server, adminEmail);

        var member = await OpenAsync(server);
        await LogInAsync(member, memberEmail, Password);
        await Expect(SignedInAs(member, memberEmail)).ToBeVisibleAsync();
        await Expect(member.GetByRole(AriaRole.Link, new() { Name = "관리", Exact = true })).ToBeHiddenAsync();

        var admin = await OpenAsync(server);
        await LogInAsync(admin, adminEmail, Password);
        await admin.GetByRole(AriaRole.Link, new() { Name = "관리", Exact = true }).ClickAsync();
        await Expect(admin.GetByRole(AriaRole.Heading, new() { Name = "권한 관리" })).ToBeVisibleAsync();

        await admin.GetByLabel("역할 이름").FillAsync("상품 편집자");
        await admin.GetByRole(AriaRole.Checkbox, new() { Name = "products.manage" }).CheckAsync();
        await admin.GetByRole(AriaRole.Button, new() { Name = "역할 만들기" }).ClickAsync();
        await Expect(Notice(admin)).ToContainTextAsync("역할을 만들었어요");

        await admin.GetByLabel("새 그룹 이름").FillAsync("편집팀");
        await admin.GetByRole(AriaRole.Button, new() { Name = "그룹 만들기" }).ClickAsync();
        await Expect(admin.GetByRole(AriaRole.Heading, new() { Name = "편집팀 그룹" })).ToBeVisibleAsync();
        await admin.GetByLabel("구성원 이메일").FillAsync(memberEmail);
        await admin.GetByRole(AriaRole.Button, new() { Name = "구성원 추가" }).ClickAsync();
        await Expect(Notice(admin)).ToContainTextAsync("구성원을 넣었어요");
        await admin.GetByRole(AriaRole.Checkbox, new() { Name = "상품 편집자" }).CheckAsync();
        await Expect(admin.GetByRole(AriaRole.Checkbox, new() { Name = "상품 편집자" })).ToBeCheckedAsync();

        await admin.GetByLabel("구성원 이메일").FillAsync("nobody-" + memberEmail);
        await admin.GetByRole(AriaRole.Button, new() { Name = "구성원 추가" }).ClickAsync();
        await Expect(Error(admin)).ToContainTextAsync("그 이메일로 가입한 사용자가 없어요");

        await member.GotoAsync("/account");
        await Expect(member.Locator("main").GetByText("products.manage")).ToBeVisibleAsync();
    }
}
