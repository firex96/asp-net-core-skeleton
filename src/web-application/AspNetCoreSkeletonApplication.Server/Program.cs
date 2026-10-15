using AspNetCoreSkeletonApplication.Common.Configuration;
using AspNetCoreSkeletonApplication.Common.Web;
using AspNetCoreSkeletonApplication.Server.Clients;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 실행 프로파일(ASPNETCORE_ENVIRONMENT)은 배포 단계가 아니라 비밀값 출처(Local / Aws / Azure / Gcp)를 고른다.
builder.AddSecretSource<Program>();

// Common(DB 컨텍스트 · EntityService · 공유 BizService) 다음에 이 앱의 MappingService · BizService 를 등록한다.
builder.Services.AddCommon();
builder.Services.AddLayeredServices(typeof(Program).Assembly);
builder.Services.AddSkeletonWeb();
// 로그인 — Identity + 쿠키(같은 출처 SPA). README 「인증」
builder.Services.AddSkeletonIdentity();

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

app.UseAuthentication();
app.UseAuthorization();
app.UseAuditorContext();

app.MapHealthChecks("/health").AllowAnonymous();

app.MapSkeletonAuthEndpoints();
app.MapControllers();

app.MapFallbackToFile("/index.html").AllowAnonymous();

app.Run();
