using AspNetCoreSkeletonApplication.Common.Configuration;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 실행 프로파일(ASPNETCORE_ENVIRONMENT)은 배포 단계가 아니라 비밀값 출처(Local / Aws / Azure / Gcp)를 고른다.
builder.AddSecretSource<Program>();

// Common(DB 컨텍스트 · EntityService · 공유 BizService) 다음에 이 앱의 MappingService · BizService 를 등록한다.
builder.Services.AddCommon();
builder.Services.AddLayeredServices(typeof(Program).Assembly);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
