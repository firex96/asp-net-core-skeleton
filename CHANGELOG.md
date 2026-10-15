# Changelog

이 프로젝트의 주요 변경을 기록한다. 형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/) 를 따른다.

## [Unreleased]

### Added
- Batch 운영 대시보드 `/quartz`(Quartz.Dashboard) — Basic 인증 운영자 정책 뒤(정적 자산 포함), 계정 미설정 시 fail-closed, 다룰 Job 타입을 앱의 `Jobs` 로 제한
- Batch 수동 실행 웹훅 `POST /api/jobs/{Job}/trigger` — API 키 인증(상수 시간 비교, 기동 시 검증), 키별 허용 Job(403), 파라미터 화이트리스트(400), Quartz `TriggerJob` 으로 실행해 동시 실행 방지 · 실행 이력 적용, 202 + 추적 ID
- 본문 없는 4xx 응답도 ProblemDetails(`UseStatusCodePages`), 에러 코드 `UNAUTHORIZED_ERROR`(401, G015)
- 샘플 Job `DiscontinueStaleDraftJob` — 오래된 Draft 상품을 판매 종료(Domain `StaleDraftPolicy`, keyset 페이지, 페이지마다 트랜잭션, 웹과 같은 상태 전이 경로). 재실행 멱등을 Quartz 실행으로 확인
- 배치 앱 `Batch` — Quartz.NET 4.5(SQL Server 영속 저장소 · 클러스터 · 실행 이력), 스키마 마이그레이션 `AddQuartzScheduler`(`quartz` 스키마, 정본 DDL), `TracedJob`(추적 ID · 감사 주체 · 로그), 구성 기반 스케줄 `Batch:Jobs:{Job}`, 템플릿 선택지 `--batch`
- 캐시 — HybridCache(메모리 L1 + Redis L2), EntityService 전용 `EntityCache`(없는 값은 캐시하지 않음, 변경 시 태그 무효화를 커밋 뒤에 실행). 비밀값 `ConnectionStrings:Redis`, 설정 `Cache:*`, docker compose 에 Redis, 통합 테스트 fixture 를 `InfrastructureFixture`(SQL Server · Redis)로
- 느린 쿼리 · 요청 경고 — `SlowQueryInterceptor`(EF 명령), `SlowRequestMiddleware`(HTTP), 임계값 `Diagnostics:SlowQueryThreshold` · `SlowRequestThreshold`. 테스트에 `FakeLogger`(`Microsoft.Extensions.Diagnostics.Testing`)
- 논리 삭제 `ISoftDeletable` — `Remove` 를 인터셉터가 삭제 주체 · 시각을 채운 수정으로 바꾸고, 전역 쿼리 필터가 지워진 행을 뺀다. `DELETE /api/products/{id}`(204)
- 추가 전용 엔티티 기반 `BaseCreateAuditedEntity`(수정 · 삭제 시 예외)와 그 사용처인 상품 상태 이력(`product_status_history`, `GET /api/products/{id}/status-history`) — 상태가 바뀔 때 같은 트랜잭션에서 기록
- 열거형 외부 코드 — 멤버의 `[JsonStringEnumMemberName]` 이 JSON · 쿼리 바인딩 · DB 컬럼 공통 코드(`EnumCodes`, `StringEnumValueConverter`, 열거형 모델 바인더). 규약 테스트로 모든 멤버에 강제
- 에러 응답을 ProblemDetails 로 통일 — `code` · `divisionCode` 확장 필드, `ErrorCode` · `CodedException`(`ItemNotFoundException` · `ClientBadRequestException` · `ConflictException`), `GlobalExceptionHandler`, 5xx 는 고정 문구
- 추적 ID `X-Trace-Id` — 받은 값을 검증해 쓰거나 새로 만들고, 로깅 scope · 응답 헤더 · 에러 응답 `traceId` 에 싣는다(`TraceIdMiddleware`, `TraceIdContext`)
- 외부 API 클라이언트 골격 `AddApiClient<TClient, TOptions>` — 구성 검증, 표준 resilience(`Microsoft.Extensions.Http.Resilience`), 추적 ID 전파, 실패 → `ApiCallException`(502 `EXTERNAL_API_ERROR`). 샘플 `ExampleApiClient`
- 감사 필드 자동 기록 — `BaseEntity` · `BaseAuditedEntity`, `AuditSaveChangesInterceptor`, 감사 주체 `AuditorContext`(인증 주체 > 신뢰된 `X-User-Id` > 시스템 `0`), `Audit:TrustUserHeader`(기본 false)
- Product 샘플 — Common 의 공유 `ProductBizService` · `ProductStatusTransition`(Domain) · `IProductEntityService`, `IMainTransaction`, 마이그레이션 `Initial`, Server 의 `/api/products` (GET · POST · PUT status). 열거형은 JSON 에서 이름 문자열
- `dotnet-ef` 로컬 도구(`dotnet-tools.json`), 단위 테스트용 NSubstitute
- 규약 테스트(`UnitTests/Conventions/`) — 계층별 주입 허용 목록, Controller 는 Model 만, 접미사 ↔ 네임스페이스, Model · Dto · 매퍼 · 엔티티 선언, Domain 순수성. README 「코딩 규약」
- 게이트웨이 뒤 내부 API 앱 `InternalApi` — Server 와 같은 Common 을 쓰고 SPA 를 서빙하지 않는다. 템플릿 선택지 `--internalApi`(기본 true)

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
