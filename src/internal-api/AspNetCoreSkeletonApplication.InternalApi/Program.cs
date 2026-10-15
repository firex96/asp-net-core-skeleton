using AspNetCoreSkeletonApplication.Common.Configuration;
using AspNetCoreSkeletonApplication.Common.Web;
using Scalar.AspNetCore;

namespace AspNetCoreSkeletonApplication.InternalApi;

/// <summary>
/// 게이트웨이 뒤의 내부 API. 구성은 Server 와 같고 SPA 서빙만 없다.
/// top-level 문 대신 네임스페이스 안의 클래스로 둔다 — 테스트 프로젝트가 Server 와 함께 참조해도
/// 전역 <c>Program</c>(Server)과 이름이 겹치지 않게 하려는 것이다.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 실행 프로파일(ASPNETCORE_ENVIRONMENT)은 배포 단계가 아니라 비밀값 출처(Local / Aws / Azure / Gcp)를 고른다.
        builder.AddSecretSource<Program>();

        // Common(DB 컨텍스트 · EntityService · 공유 BizService) 다음에 이 앱의 MappingService · BizService 를 등록한다.
        builder.Services.AddCommon();
        builder.Services.AddLayeredServices(typeof(Program).Assembly);
        builder.Services.AddSkeletonWeb();

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

        app.UseAuthorization();
        app.UseAuditorContext();

        app.MapHealthChecks("/health");

        app.MapControllers();

        app.Run();
    }
}
