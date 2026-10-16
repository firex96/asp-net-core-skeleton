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
}
