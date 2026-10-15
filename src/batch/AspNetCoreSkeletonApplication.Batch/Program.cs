using AspNetCoreSkeletonApplication.Batch.Configuration;
using AspNetCoreSkeletonApplication.Batch.Jobs;
using AspNetCoreSkeletonApplication.Batch.Security;
using AspNetCoreSkeletonApplication.Common.Configuration;
using AspNetCoreSkeletonApplication.Common.Web;
using Quartz;
using Scalar.AspNetCore;

namespace AspNetCoreSkeletonApplication.Batch;

/// <summary>
/// 배치 앱. Quartz.NET 으로 Job 을 스케줄한다. 구성은 다른 앱과 같고 SPA 서빙만 없다.
/// top-level 문 대신 네임스페이스 안의 클래스 — 테스트가 다른 앱과 함께 참조해도 <c>Program</c> 이름이 겹치지 않는다.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 실행 프로파일(ASPNETCORE_ENVIRONMENT)은 배포 단계가 아니라 비밀값 출처(Local / Aws / Azure / Gcp)를 고른다.
        builder.AddSecretSource<Program>();

        builder.Services.AddCommon();
        builder.Services.AddLayeredServices(typeof(Program).Assembly);
        builder.Services.AddSkeletonWeb();

        // 스케줄할 Job — 스케줄은 Batch:Jobs:{Job 클래스 이름} 구성이 정한다.
        builder.Services.AddBatchScheduler(builder.Configuration, jobs => jobs
            .Add<DiscontinueStaleDraftJob>());
        builder.Services.AddOptions<DiscontinueStaleDraftJobOptions>()
            .BindConfiguration(BatchSchedulerExtensions.SectionOf(nameof(DiscontinueStaleDraftJob)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddBatchSecurity();

        // 운영 대시보드(/quartz) — 운영자 정책 뒤에, 이 앱의 Job 타입만 다룬다(임의 타입 — 예: 실행 파일을 띄우는 NativeJob — 은 막는다).
        builder.Services.AddQuartzDashboard(options =>
        {
            options.AuthorizationPolicy = BatchPolicies.Operator;
            options.IsJobTypeAllowed = typeName => typeName.StartsWith(typeof(Program).Namespace + ".Jobs.", StringComparison.Ordinal);
        });

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.UseSkeletonWeb();

        if (app.Configuration.GetValue<bool>("OpenApi:Enabled"))
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAuditorContext();
        // 대시보드(Blazor)의 폼 · 회로가 쓴다 — 인증 · 인가 뒤에 둔다.
        app.UseAntiforgery();

        app.MapHealthChecks("/health");

        app.MapControllers();
        // 대시보드의 Blazor 스크립트 · 패키지 CSS 같은 정적 자산 — 대시보드와 같은 운영자 정책 뒤에 둔다.
        app.MapStaticAssets().RequireAuthorization(BatchPolicies.Operator);
        app.MapQuartzDashboard().RequireAuthorization(BatchPolicies.Operator);

        app.Run();
    }
}
