# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

이 레포는 **ASP.NET Core + SPA 스켈레톤**이다. 사람용 설명의 정본은 [README.md](README.md) 이고, 여기에는 작업할 때 바로 필요한 명령과 규칙만 둔다.

## Build and Development Commands

```bash
# 빌드 (SPA esproj 포함 — 각 SPA 에 node_modules 가 있어야 한다)
dotnet build AspNetCoreSkeletonApplication.slnx

# 테스트 — IntegrationTests 는 Docker 필요
dotnet test --solution AspNetCoreSkeletonApplication.slnx
dotnet test --project test/unit-tests/AspNetCoreSkeletonApplication.UnitTests
dotnet test --project test/integration-tests/AspNetCoreSkeletonApplication.IntegrationTests -- --filter-class "*HealthCheckTests"

# 로컬 실행 — Local 프로파일 + SpaProxy (SPA 는 SpaClient 로 고른다, 기본 angular)
docker compose up -d
dotnet run --project src/web-application/AspNetCoreSkeletonApplication.Server --launch-profile https
dotnet run --project src/web-application/AspNetCoreSkeletonApplication.Server --launch-profile https -p:SpaClient=nuxt

# SPA (각 src/spa/<클라이언트> 에서)
npm ci && npm run build && npm test          # Angular 는 npm test -- --watch=false
npm run check                                # SvelteKit · Nuxt 타입 검사

# 템플릿 확인 — 사용자 템플릿 캐시를 건드리지 않게 custom hive 를 쓴다
dotnet new install . --debug:custom-hive <임시 폴더>
dotnet new aspnet-spa-skeleton -n Contoso.Shop --frontend all -o <출력> --debug:custom-hive <임시 폴더>
```

## Architecture

- 프로젝트는 공유·배포 단위다: `Common`(공유 라이브러리) ← 앱(`Server`). 계층은 프로젝트 안의 네임스페이스다 — `Controller → MappingService → BizService → EntityService → DbContext`, 업무 판단은 순수 `Domain`. 계층별 책임·주입 허용 목록은 README 「요청 흐름」이 정본이다.
- Entity · `AppDbContext` · Entity 매퍼는 `Common` 안에서 `internal` 이다. 앱은 EntityService 의 Dto 로만 데이터를 본다.
- 컴포지션 루트는 각 앱의 `Program.cs` 다. 계층 서비스는 `AddLayeredServices` 가 이름 접미사로 등록하므로 따로 등록하지 않는다.
- 비밀값 출처 분기는 `Common/Configuration/SecretSourceExtensions.cs` 한 곳이다. 프로파일(`ASPNETCORE_ENVIRONMENT`)은 Local · Aws · Azure · Gcp 와 미지정(환경변수만)뿐이다 — dev·staging·prod 프로파일을 만들지 않는다.
- SPA 선택은 서버 csproj 의 `SpaClient` 속성과 `Choose` 블록이다. SPA 를 추가·삭제하면 csproj `Choose`, `AspNetCoreSkeletonApplication.slnx`, `.template.config/template.json` 의 `frontend` 선택지·`modifiers` 를 함께 고친다.

## 규칙

- **비밀값은 구성 키로만 읽는다.** 코드는 `ConnectionStrings:Default` 처럼 .NET 구성 키만 쓰고, 값은 환경변수(`__`) 또는 비밀 저장소(`--`, AWS 는 JSON 키)가 채운다. 평문 자격증명은 어느 appsettings 에도 적지 않는다 — `appsettings.Local.json` 의 docker compose 계정만 예외. 비밀을 추가하면 README 「설정과 비밀값」과 `appsettings.Gcp.json` 의 `Secrets` 를 함께 고친다.
- **필수 값은 폴백 없이 엄격하게.** 빠진 비밀은 기동 실패로 드러나야 한다(`GetRequiredConnectionString` 참고). 빈 문자열 기본값을 두지 않는다.
- **패키지 버전은 `Directory.Packages.props` 에만.** csproj 의 `PackageReference` 에 `Version` 을 적지 않는다. 테스트 공통 설정은 `test/Directory.Build.props`.
- **새 테스트는 의존으로 위치를 정한다.** 외부 의존 없음 → `UnitTests`, HTTP 파이프라인(DB 없음) → `FunctionalTests`, Docker(DB·클라우드 에뮬레이터) → `IntegrationTests`.
- **템플릿 조건 주석을 깨지 않는다.** 서버 csproj 와 slnx 의 `<!--#if (frontend == …) -->` 는 `dotnet new` 용이다. 레포 원본에서는 그냥 주석이므로 첫 분기(angular)가 기본값이 되도록 순서를 지킨다.
- 이름은 `AspNetCoreSkeletonApplication`(PascalCase)과 `asp-net-core-skeleton-application`(kebab-case) 두 형태로만 쓴다 — 템플릿이 이 두 문자열을 치환한다.
- C# 은 file-scoped namespace, 생성자 주입은 primary constructor 를 쓴다. 들여쓰기는 `.editorconfig`.
