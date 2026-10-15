# Changelog

이 프로젝트의 주요 변경을 기록한다. 형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/) 를 따른다.

## [Unreleased]

### Added
- 에러 응답을 ProblemDetails 로 통일 — `code` · `divisionCode` 확장 필드, `ErrorCode` · `CodedException`(`ItemNotFoundException` · `ClientBadRequestException` · `ConflictException`), `GlobalExceptionHandler`, 5xx 는 고정 문구
- 추적 ID `X-Trace-Id` — 받은 값을 검증해 쓰거나 새로 만들고, 로깅 scope · 응답 헤더 · 에러 응답 `traceId` 에 싣는다(`TraceIdMiddleware`, `TraceIdContext`)

### Changed
- 프로젝트를 공유·배포 단위로 재편 — 빈 `ApplicationCore` · `Infrastructure.*` 를 지우고 공유 라이브러리 `Common` 을 둔다. `AppDbContext`(internal) 와 비밀값 출처 구성(`AddSecretSource<TUserSecrets>()`)이 `Common` 으로 옮겨졌다
- 계층 `Controller → MappingService → BizService → EntityService` 를 도입하고 WeatherForecast 를 이 흐름으로 옮겼다(응답 JSON 은 같다). 계층 서비스는 `AddLayeredServices` 가 이름 접미사로 등록한다
- Mapperly 엄격 모드 — 대상 누락(RMG012)·빈 매핑(RMG066)은 빌드 오류, 속성 null 불일치는 런타임 예외
- 조회 기본 추적 모드를 NoTracking 으로

### Removed
- 쓰지 않던 `LanguageExt.Core` · `FluentValidation` 패키지

## [0.1.0] - 2026-10-15

### Added
- ASP.NET Core 10 Web API + SPA 솔루션 (`.slnx`, 중앙 패키지 관리, `global.json`)
- SPA 클라이언트 셋 — Angular 22 · SvelteKit 3 · Nuxt 4 — 와 서버 csproj `SpaClient` 속성 전환
- 비밀값 출처 프로파일 Local · Aws · Azure · Gcp (`SecretSourceExtensions`), `ConnectionStrings:Default` 누락 시 기동 실패
- Local 용 `docker-compose.yml` (SQL Server 2022), `/health` DB 헬스 체크
- 내장 OpenAPI 문서 + Scalar UI (`OpenApi:Enabled`)
- 테스트 3분류 — UnitTests · FunctionalTests(`WebApplicationFactory`) · IntegrationTests(Testcontainers SQL Server · LocalStack) — xUnit v3 + Microsoft.Testing.Platform
- `dotnet new aspnet-spa-skeleton` 템플릿 (`--frontend angular|sveltekit|nuxt|all`)
