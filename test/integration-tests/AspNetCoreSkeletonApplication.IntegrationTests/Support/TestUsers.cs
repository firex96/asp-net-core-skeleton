using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Common.Authorization.Services.Biz;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Support;

/// <summary>Server 에 실제로 가입 · 로그인한 클라이언트(쿠키)를 만든다. 쿠키가 Secure 라 https 주소로 붙는다.</summary>
internal static class TestUsers
{
    public const string Password = "Passw0rd!-long";

    /// <summary>마이그레이션 <c>AddAuthorizationGroups</c> 가 시드한 관리자 그룹.</summary>
    public const string AdministratorsGroup = "Administrators";

    public static readonly WebApplicationFactoryClientOptions Https = new() { BaseAddress = new Uri("https://localhost") };

    public static async Task<(HttpClient Client, long UserId)> SignInAsync(
        WebApplicationFactory<Program> factory, string email, bool administrator)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = factory.CreateClient(Https);
        (await client.PostAsJsonAsync("/api/auth/register", new { email, password = Password }, cancellationToken)).EnsureSuccessStatusCode();
        await ConfirmEmailAsync(factory, email);

        if (administrator)
        {
            // 마이그레이션이 시드한 Administrators 그룹(역할 Administrator — 모든 권한)에 넣는다. BizService 를 거쳐 캐시도 무효화된다.
            using var scope = factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email) ?? throw new InvalidOperationException($"{email} 가 없다");
            var groups = scope.ServiceProvider.GetRequiredService<UserGroupBizService>();
            var administrators = (await groups.ListAsync(cancellationToken)).Single(group => group.Name == AdministratorsGroup);
            await groups.AddMemberAsync(administrators.Id, user.Id, cancellationToken);
        }

        (await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password }, cancellationToken)).EnsureSuccessStatusCode();
        var me = JsonDocument.Parse(await client.GetStringAsync("/api/auth/me", cancellationToken)).RootElement;
        return (client, me.GetProperty("id").GetInt64());
    }

    /// <summary>메일 링크를 거치지 않고 이메일을 확인한다 — 링크가 시험 대상이 아닌 테스트에서. 토큰은 링크와 같은 Identity 토큰이다.</summary>
    public static async Task ConfirmEmailAsync(WebApplicationFactory<Program> factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email) ?? throw new InvalidOperationException($"{email} 가 없다");
        var result = await users.ConfirmEmailAsync(user, await users.GenerateEmailConfirmationTokenAsync(user));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"{email} 를 확인하지 못했다");
        }
    }
}
