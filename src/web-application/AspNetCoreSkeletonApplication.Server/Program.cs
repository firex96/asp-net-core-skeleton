using AspNetCoreSkeletonApplication.Common.Configuration;
using AspNetCoreSkeletonApplication.Common.Mail;
using AspNetCoreSkeletonApplication.Common.Web;
using AspNetCoreSkeletonApplication.Common.Web.OAuth;
using AspNetCoreSkeletonApplication.Server.Example.Clients;
using AspNetCoreSkeletonApplication.Server.Product.Authorization;
using Microsoft.AspNetCore.Authorization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 실행 프로파일(ASPNETCORE_ENVIRONMENT)은 배포 단계가 아니라 비밀값 출처(Local / Aws / Azure / Gcp)를 고른다.
builder.AddSecretSource<Program>();

// Common(DB 컨텍스트 · EntityService · 공유 BizService) 다음에 이 앱의 MappingService · BizService 를 등록한다.
builder.Services.AddCommon();
builder.Services.AddLayeredServices(typeof(Program).Assembly);
builder.Services.AddSkeletonWeb();
// 로그인 — Identity + 쿠키(같은 출처 SPA), 권한 인가(그룹 · 역할 · 권한). README 「인증」 · 「인가」
builder.Services.AddSkeletonIdentity();
// 소셜 로그인 — 구성에 섹션이 있는 공급자만(Google · Kakao · Naver). README 「인증」
builder.Services.AddSkeletonExternalLogins(builder.Configuration);
// OAuth 인가 서버 + OIDC — 외부 클라이언트에 토큰을 발급한다. README 「OAuth 서버」
builder.Services.AddSkeletonOAuthServer(builder.Configuration);
// 계정 메일(비밀번호 재설정) — SMTP. README 「메일」
builder.Services.AddSkeletonMail();
// 리소스 기반 인가 — 데이터에 따라 갈리는 판정(작성자 본인 등)
builder.Services.AddScoped<IAuthorizationHandler, ProductAuthorizationHandler>();

// 외부 API 클라이언트 — 구성 바인딩 · 재시도 · 추적 ID 전파 · 실패 → ApiCallException
builder.Services.AddApiClient<ExampleApiClient, ExampleApiOptions>("Clients:ExampleApi");

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseSkeletonWeb();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.UseHttpsRedirection();

// 다른 출처의 브라우저 앱이 OAuth 엔드포인트를 부를 수 있게(구성 AllowedOrigins) — OpenIddict 가 처리하기 전, 인증 앞에 둔다.
app.UseSkeletonOAuthServerCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseAuditorContext();

app.MapHealthChecks("/health").AllowAnonymous();

app.MapSkeletonAuthEndpoints();
app.MapSkeletonTwoFactorEndpoints();
app.MapSkeletonPasskeyEndpoints();
app.MapSkeletonExternalLoginEndpoints();
app.MapSkeletonOAuthServerEndpoints();
app.MapControllers();

app.MapFallbackToFile("/index.html").AllowAnonymous();

app.Run();
