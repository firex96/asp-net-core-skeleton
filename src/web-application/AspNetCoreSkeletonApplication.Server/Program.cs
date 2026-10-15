using AspNetCoreSkeletonApplication.Infrastructure.EntityFramework.Context;
using AspNetCoreSkeletonApplication.Server.Configuration;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 실행 프로파일(ASPNETCORE_ENVIRONMENT)은 배포 단계가 아니라 비밀값 출처(Local / Aws / Azure / Gcp)를 고른다.
builder.AddSecretSource();

builder.Services.AddDbContextPool<AppDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetRequiredConnectionString("Default")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

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
