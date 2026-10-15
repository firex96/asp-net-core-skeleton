using System.Security.Claims;
using AspNetCoreSkeletonApplication.Common.Persistence.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace AspNetCoreSkeletonApplication.Common.Web;

/// <summary>
/// 가입 · 로그인 · 로그아웃 · 현재 사용자 — <c>/api/auth</c>. Identity 를 직접 다루는 프레임워크 구성이라 계층 규칙(Controller → MappingService …)
/// 밖에 둔다(GlobalExceptionHandler 와 같은 자리). <c>MapIdentityApi</c> 는 쓰지 않는다 — Bearer 토큰 · 메일 흐름이 딸려 온다.
/// </summary>
public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapSkeletonAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/auth");
        auth.MapPost("/register", RegisterAsync).AllowAnonymous();
        auth.MapPost("/login", LoginAsync).AllowAnonymous();
        auth.MapPost("/logout", LogoutAsync).RequireAuthorization();
        auth.MapGet("/me", Me).RequireAuthorization();
        return endpoints;
    }

    private static async Task<Results<Created, ValidationProblem>> RegisterAsync(
        CredentialsRequest request, UserManager<ApplicationUser> userManager)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);
        return result.Succeeded
            ? TypedResults.Created("/api/auth/me")
            : TypedResults.ValidationProblem(result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));
    }

    /// <summary>틀린 이메일 · 비밀번호는 같은 401 이다(계정이 있는지 드러내지 않는다). 5번 틀리면 잠기고 423 이다.</summary>
    private static async Task<Results<NoContent, ProblemHttpResult>> LoginAsync(
        CredentialsRequest request, SignInManager<ApplicationUser> signInManager)
    {
        var result = await signInManager.PasswordSignInAsync(request.Email, request.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return TypedResults.NoContent();
        }

        return result.IsLockedOut
            ? TypedResults.Problem(statusCode: StatusCodes.Status423Locked, detail: "로그인을 여러 번 실패해 계정이 잠겼다. 잠시 뒤에 다시 시도한다")
            : TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "이메일 또는 비밀번호가 맞지 않는다");
    }

    private static async Task<NoContent> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.NoContent();
    }

    private static Ok<CurrentUserResponse> Me(ClaimsPrincipal user) =>
        TypedResults.Ok(new CurrentUserResponse(
            long.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture),
            user.FindFirstValue(ClaimTypes.Email) ?? user.Identity!.Name!,
            user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()));

    public sealed record CredentialsRequest(string Email, string Password);

    public sealed record CurrentUserResponse(long Id, string Email, IReadOnlyList<string> Roles);
}
