using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreSkeletonApplication.Common.Constants;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSkeletonApplication.IntegrationTests.Support;

/// <summary>Server 에 실제로 가입 · 로그인한 클라이언트(쿠키)를 만든다. 쿠키가 Secure 라 https 주소로 붙는다.</summary>
internal static class TestUsers
{
    public const string Password = "Passw0rd!-long";

    public static readonly WebApplicationFactoryClientOptions Https = new() { BaseAddress = new Uri("https://localhost") };

    public static async Task<(HttpClient Client, long UserId)> SignInAsync(
        WebApplicationFactory<Program> factory, string email, bool administrator)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = factory.CreateClient(Https);
        (await client.PostAsJsonAsync("/api/auth/register", new { email, password = Password }, cancellationToken)).EnsureSuccessStatusCode();

        if (administrator)
        {
            using var scope = factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email) ?? throw new InvalidOperationException($"{email} 가 없다");
            Assert.True((await users.AddToRoleAsync(user, Roles.Administrator)).Succeeded);
        }

        (await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password }, cancellationToken)).EnsureSuccessStatusCode();
        var me = JsonDocument.Parse(await client.GetStringAsync("/api/auth/me", cancellationToken)).RootElement;
        return (client, me.GetProperty("id").GetInt64());
    }
}
