# Changelog

이 프로젝트의 주요 변경을 기록한다. 형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/) 를 따른다.

## [Unreleased]

### Added
- 메일 아웃박스 — 계정 메일을 `mail_outbox`(마이그레이션 `AddMailOutbox`)에 넣고 Server 의 백그라운드(`MailOutboxWorker`)가 SMTP 로 보낸다. `UPDLOCK · READPAST` 로 잡아 여러 인스턴스에서도 한 통은 한 곳만, 잡은 동안 `Lease`(5분), 실패하면 30초부터 두 배씩 8번까지 다시 시도(`MailRetryPolicy`)한 뒤 포기, 보냈거나 포기하면 본문을 비운다. 구성 `Mail:Outbox`(`PollInterval` · `BatchSize` · `Lease`)
- 브라우저 테스트 `E2ETests`(Playwright for .NET 1.63, Chromium) — SPA 를 정적 빌드해 Server(Kestrel, 자체 서명 HTTPS)로 같은 출처 서빙, SPA 마다 같은 시나리오: 가입 검증 · 가입 · 로그아웃, 5회 실패 잠금, 메일 링크로 비밀번호 재설정, 소셜 계정 연결 · 로그인 · 끊기, OAuth 인가 코드(로그인 · 동의 화면 → 토큰). 실패하면 Playwright 트레이스를 남긴다
- SPA 세 개의 계정 화면 — 비밀번호 찾기 · 재설정(메일 링크), 내 계정(권한, 소셜 계정 연결 · 끊기), OAuth 동의(같은 authorize 요청을 허용 · 거부로 POST), 로그인 화면의 소셜 버튼(폼 POST)과 결과 안내, 로그인 뒤 `/connect/…` 로 돌아갈 때는 브라우저 이동. dev 서버가 `/connect` · `/.well-known` 도 프록시하고, Nuxt 프록시는 Host 를 바꾸지 않는다
- SPA 세 개의 로그인 화면 — `/login`(로그인 · 가입 전환, ProblemDetails 오류 · 검증 메시지, 잠금 안내), 상단 바의 로그인 상태 · 로그아웃, `returnUrl` 은 같은 출처 경로만. dev 서버가 `/api` 도 백엔드로 프록시한다. 인증 모듈 단위 테스트(Angular `AuthService`, SvelteKit · Nuxt `auth.ts`)
- Batch Job `PruneOAuthTokenJob` — OAuth 서버의 만료 · 폐기 토큰과 쓰이지 않는 동의를 매일 정리(보관 `RetentionDays` 14일, OpenIddict 정리 규칙). Common OAuth 도메인(`OAuthTokenBizService` · `IOAuthTokenEntityService`), EntityService 주입 허용 목록에 OpenIddict 관리자
- Bearer 토큰으로 API 호출 — 정책 스킴(`SessionOrBearer`)이 Bearer 헤더면 OpenIddict 검증, 아니면 세션 쿠키. 권한은 `RequestPermissions` 한 곳에서 — 사용자 토큰은 사용자 권한 ∩ scope, 클라이언트 토큰은 scope. 감사 주체는 사용자 토큰이면 그 사용자, 클라이언트 토큰이면 시스템(로그 scope `ClientId`). `/api/auth/me` 는 토큰이면 scope 와의 교집합, 클라이언트 토큰이면 403
- OAuth 2.0 인가 서버 + OpenID Connect(OpenIddict 7.7) — `/connect/authorize`(PKCE 필수, 로그인은 SPA 화면, Explicit 동의는 SPA `/consent` 와 영구 authorization) · `token`(인가 코드 · 리프레시 회전 · 클라이언트 자격 증명) · `userinfo` · `endsession`, 탐색 문서, `GET /api/oauth/consent`. scope 는 OIDC 표준과 권한 코드 카탈로그. 클라이언트는 구성(`Authentication:OAuthServer:Clients`)으로 기동할 때 동기화, 키는 `KeySource`(Certificates · Ephemeral). 저장소 `oauth_*`(마이그레이션 `AddOAuthServer`)
- 소셜 로그인 — Google · Kakao · Naver(구성 섹션이 있는 공급자만, `ClientId` · `ClientSecret` 필수). 연결된 계정만 로그인(자동 가입 없음), 연결은 로그인한 사용자가. `/api/auth/external` providers · `{provider}/login` · `{provider}/link`(POST 시작) · callback · logins · 끊기(마지막 수단 409 `LAST_LOGIN_METHOD_ERROR`). 에러 코드 `EXTERNAL_LOGIN_NOT_LINKED_ERROR`(AUTH003) · `EXTERNAL_LOGIN_IN_USE_ERROR`(AUTH004). docker compose 와 통합 테스트에 mock OAuth 서버(`docker/mock-oauth2-server.json`)
- 메일과 비밀번호 재설정 — `Common/Mail`(MailKit SMTP, 언제나 인증, 본문은 로그에 남기지 않음), `POST /api/auth/password/forgot`(항상 202) · `reset`(2시간 · 1회용 토큰, 400 `INVALID_TOKEN_ERROR` AUTH002, 성공 시 잠금 해제 · 다른 세션 1분 안에 무효). 필수 구성 `App:PublicOrigin` · `Mail:Smtp:*`(비밀값 `Mail:Smtp:Password`), docker compose 와 통합 테스트에 Mailpit, 템플릿이 고른 SPA 의 출처를 `App:PublicOrigin` 에 넣는다
- 권한 인가 — 사용자 → 그룹 → 역할 → 권한 코드(`user_group` · `user_group_member` · `role` · `user_group_role` · `role_permission`, 마이그레이션 `AddAuthorizationGroups` 가 기존 Administrator 역할 사용자를 `Administrators` 그룹으로 옮긴다). `[HasPermission(Permissions.…)]` 과 동적 정책 제공자, 판정 입구 `IPermissionEvaluator`(요청마다 서버 판정, 캐시 + 백플레인으로 즉시 반영), 관리 API `/api/authorization/groups` · `roles` · `permissions`(마지막 관리자 보호 409), `/api/auth/me` 가 실효 권한을 돌려준다. 계정 조회용 Account 도메인(`UserAccountBizService`)
- 리소스 기반 인가 샘플 — `ProductAuthorizationHandler` + Domain `ProductAccessRule`: 관리 권한이 있거나 자기 Draft 상품이면 삭제할 수 있다. 상품 응답에 `createdBy` 를 싣는다
- 여러 인스턴스의 캐시 즉시 무효화 — `HybridCache` 구현을 FusionCache 로 바꾸고(`AsHybridCache()`) Redis 백플레인을 붙인다. 한 인스턴스의 키 · 태그 무효화가 다른 인스턴스의 L1 에서도 곧바로 지워진다. `Cache:LocalExpiration` 은 없앤다(L1 · L2 공통 `Cache:Expiration`), 캐시 키는 `{KeyPrefix}cache:` 아래
- 인가 — 기본 정책은 로그인 필요(익명은 `[AllowAnonymous]` 로 명시), 관리자 정책 `Policies.Administrator`(Product 상태 변경 · 삭제), 규약 테스트로 역할 문자열 인가 금지
- CSRF 방어 — Fetch Metadata(`Sec-Fetch-Site: cross-site` 인 변경 요청 403), SameSite=Strict 쿠키와 겹쳐 둔다
- 로그인 — ASP.NET Core Identity + 쿠키(`__Host-session`, HttpOnly · Secure · SameSite=Strict), `/api/auth` register · login · logout · me, 5회 실패 시 30분 잠금(423 `ACCOUNT_LOCKED_ERROR`), 역할 `Administrator` 시드, 계정 테이블 `identity_*`(마이그레이션 `AddIdentity`)
- Data Protection 키를 Redis 에 저장 — 여러 인스턴스가 쿠키 · antiforgery 토큰을 함께 읽는다
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
- 메일 전송을 Mail 도메인의 클라이언트 계층으로 — `IMailSender` · `SmtpMailSender` → `IMailClient` · `SmtpMailClient`(`Mail.Clients`), `OutgoingMail` → `OutgoingMailDto`. Identity 계정 메일(`IdentityEmailSender`)은 아웃박스에 넣는다
- SPA 세 개의 화면을 다듬었다 — 의존성 없는 공통 CSS(카드형 계정 화면, 상단 바, 버튼 · 입력, 공급자 색 소셜 버튼, 알림, 라이트 · 다크), 화면 문구를 자연스러운 한국어로(`lang="ko"`, 화면별 제목), 검증 오류는 같은 문장을 한 번만, 로그인 화면에서는 상단 로그인 버튼을 숨긴다
- 사용자에게 보이는 서버 문구를 자연스러운 한국어로 — 로그인 실패 · 잠금 · 재설정 링크 · 마지막 로그인 수단 메시지, 비밀번호 규칙 · 이메일 중복 같은 Identity 오류(`KoreanIdentityErrorDescriber`, 코드는 그대로), 재설정 메일, 동의 화면의 scope · 권한 설명, 소셜 공급자 표시 이름(카카오 · 네이버)
- 업무 코드를 도메인 패키지로 — 네임스페이스는 `{프로젝트}.{도메인}.{계층}`(`Common.Product.Persistence.Services`), 공통 기반 영역에는 abstract 기반 타입만. 규약 테스트 `DomainConventionTests`(다른 도메인의 영속 계층 참조 금지) · `TableNamingConventionTests`(테이블은 클래스 이름의 snake_case, 예약어 금지)
- 최소 API 의 검증 실패(`HttpValidationProblemDetails`)도 `NOT_VALID_ERROR` 로 분류
- 프로젝트를 공유·배포 단위로 재편 — 빈 `ApplicationCore` · `Infrastructure.*` 를 지우고 공유 라이브러리 `Common` 을 둔다. `AppDbContext`(internal) 와 비밀값 출처 구성(`AddSecretSource<TUserSecrets>()`)이 `Common` 으로 옮겨졌다
- 계층 `Controller → MappingService → BizService → EntityService` 를 도입하고 WeatherForecast 를 이 흐름으로 옮겼다(응답 JSON 은 같다). 계층 서비스는 `AddLayeredServices` 가 이름 접미사로 등록한다
- Mapperly 엄격 모드 — 대상 누락(RMG012)·빈 매핑(RMG066)은 빌드 오류, 속성 null 불일치는 런타임 예외
- 조회 기본 추적 모드를 NoTracking 으로

### Removed
- Identity 역할(`identity_role` · `identity_user_role` · `identity_role_claim`, `ApplicationRole`), `Roles` · `Policies.Administrator` — 인가 도메인의 그룹 · 역할 · 권한으로 바뀌었다
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
