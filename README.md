# ASP.NET Core + SPA Skeleton

**ASP.NET Core Web API 와 SPA 를 한 솔루션으로 시작할 때 쓰는 템플릿이다.** .NET 10 백엔드, 프론트엔드 선택지(Angular · SvelteKit · Nuxt), 레이어 구조, 의존으로 나눈 테스트 4분류(단위 · 기능 · 통합 · 브라우저), 클라우드별 비밀값 출처 프로파일(Local · Aws · Azure · Gcp)이 이미 배선돼 있다.

이 문서는 **이 스켈레톤으로 새 프로젝트를 만드는 사람**을 대상으로 한다. 위에서부터 읽고 「시작하기」를 따라 하면 새 프로젝트가 뜬다.

## 형제 스켈레톤

같은 원칙(프로파일은 비밀값 출처만 고른다, 테스트는 의존으로 나눈다)을 쓰는 Spring 스켈레톤이 셋 있다. 백엔드 언어와 용도에 맞춰 고른다.

| 스켈레톤 | 쓰는 곳 |
|---|---|
| **`asp-net-core-skeleton`** (이 레포) | ASP.NET Core API + SPA 한 벌, 같은 Common 을 쓰는 내부 API · 배치(선택) |
| `spring-boot-public-api-skeleton` | 외부에 노출되는 API (Spring Security + JWT) |
| `spring-boot-internal-api-skeleton` | 게이트웨이 뒤의 내부 API |
| `spring-batch-skeleton` | 배치·스케줄 작업 |

## 기술 스택

| 항목 | 버전 |
|---|---|
| .NET SDK / ASP.NET Core | 10.0.400 / 10.0 (`global.json`) |
| EF Core (SQL Server) | 10.0.13 |
| OpenAPI 문서 · UI | `Microsoft.AspNetCore.OpenApi` · Scalar |
| 테스트 | xUnit v3 + Microsoft.Testing.Platform, Testcontainers, Playwright for .NET 1.63 (Chromium) |
| Node.js | 24 (각 SPA `package.json` 의 `volta.node`) |
| Angular | 22.2 (Vitest) |
| SvelteKit / Svelte | 3.0 / 5 (adapter-static) |
| Nuxt / Vue | 4.6 / 3.5 (`ssr: false`) |
| 로컬 인프라 | SQL Server 2022 · Redis 7.4 · Mailpit · mock OAuth 컨테이너 (`docker-compose.yml`) |
| 소셜 로그인 | Google(Microsoft) · Kakao · Naver(aspnet-contrib) OAuth 핸들러 |
| 메일 | MailKit (SMTP) |
| 캐시 | FusionCache (`HybridCache` 로 노출) — 메모리 L1 + Redis L2 + Redis 백플레인 |

**여기 적힌 숫자는 빌드 파일이 정본이다.** 어긋나면 `global.json` · `Directory.Packages.props` · 각 SPA `package.json` 을 따른다.

## 시작하기

### 1. 사전 준비

```bash
dotnet --version        # 10.0.400 이상
node --version          # 24.15 이상 (Angular 22 CLI 요구) — Volta 를 쓰면 package.json 의 고정 버전이 자동으로 잡힌다
docker info             # 로컬 실행(docker compose)과 통합 · 브라우저 테스트(Testcontainers)에 쓴다
dotnet dev-certs https --trust   # 개발 인증서 신뢰 — SPA dev 서버도 이 인증서로 HTTPS 를 연다
```

### 2. 새 프로젝트를 만든다

**`dotnet new` 템플릿으로 (권장)** — 이름이 모두 바뀌고, 고른 SPA 만 들어간다.

```bash
git clone https://github.com/firex96/asp-net-core-skeleton.git
dotnet new install ./asp-net-core-skeleton
dotnet new aspnet-spa-skeleton -n Contoso.Shop --frontend sveltekit
```

| 옵션 | 값 | 기본 |
|---|---|---|
| `-n` | 솔루션·프로젝트 이름. `AspNetCoreSkeletonApplication` 은 이 값으로, `asp-net-core-skeleton-application` 은 kebab-case(`contoso-shop`)로 바뀐다 | 출력 폴더 이름 |
| `--frontend` | `angular` · `sveltekit` · `nuxt` · `all`(셋 다 두고 `SpaClient` 로 전환) | `angular` |
| `--internalApi` | 게이트웨이 뒤 내부 API 앱(`InternalApi`)을 함께 만든다. `false` 면 앱과 테스트의 `InternalApi/` 폴더가 빠진다 | `true` |
| `--batch` | 배치 앱(`Batch` — Quartz.NET)을 함께 만든다. `false` 면 앱과 테스트의 `Batch/` 폴더가 빠진다(DB 의 `quartz` 스키마는 남는다 — 쓰지 않는다) | `true` |

user-secrets ID 도 새로 만들어진다. 템플릿을 지우려면 `dotnet new uninstall <경로>`.

**복사해서** 쓴다면 `AspNetCoreSkeletonApplication`(PascalCase)과 `asp-net-core-skeleton-application`(kebab-case)을 폴더·파일 이름과 내용에서 모두 바꾸고, 세 앱 csproj(Server · InternalApi · Batch)의 `UserSecretsId` 를 같은 새 GUID 로 바꾼다. 쓰지 않는 SPA 는 「프론트엔드 선택」의 표대로 지운다.

### 3. 프론트엔드를 고른다

`src/spa/` 의 SPA 중 하나를 서버 csproj 의 `SpaClient` 속성으로 고른다(기본 `angular`). 개발 중에는 SpaProxy 가 고른 SPA 의 dev 서버를 띄우고, 게시(`dotnet publish`)하면 그 SPA 의 정적 빌드만 `wwwroot` 에 들어간다.

| `SpaClient` | 폴더 | dev 서버 | 정적 빌드 출력 |
|---|---|---|---|
| `angular` | `asp-net-core-skeleton-application.client.angular` | https://localhost:61194 | `dist/<이름>/browser` |
| `sveltekit` | `asp-net-core-skeleton-application.client.sveltekit` | https://localhost:61195 | `build` |
| `nuxt` | `asp-net-core-skeleton-application.client.nuxt` | https://localhost:61196 | `.output/public` |

```bash
cd src/web-application/AspNetCoreSkeletonApplication.Server
dotnet run --launch-profile https -p:SpaClient=sveltekit
SpaClient=nuxt dotnet run --launch-profile https   # 환경 변수로도 된다
```

SPA 를 하나만 쓴다면 나머지 폴더를 지우고, 서버 csproj 의 `Choose` 에서 해당 `When` 과 `AspNetCoreSkeletonApplication.slnx` 의 해당 `Project` 를 지운다. 기본값이 지운 SPA 였다면 `SpaClient` 기본값도 바꾼다.

세 SPA 는 같은 예제(`/weatherforecast` 표)와 같은 계정 화면을 같은 방식으로 붙인다 — `/login`(로그인 · 가입 · 소셜 로그인 버튼), `/forgot-password` · `/reset-password`(메일 링크로 재설정), `/account`(권한 · 소셜 계정 연결 · 끊기), `/consent`(OAuth 동의), 상단 바의 로그인 상태 · 로그아웃. dev 서버는 `dotnet dev-certs` 인증서로 HTTPS 를 열고, `/weatherforecast` · `/api` · `/connect` · `/.well-known` 을 ASP.NET Core 로 프록시한다(`ASPNETCORE_HTTPS_PORT` · `ASPNETCORE_URLS` 로 대상 결정). API 를 dev 서버 출처로 프록시하므로 세션 쿠키가 SPA 와 같은 출처에 저장되고, Fetch Metadata 검사도 same-origin 으로 통과한다. 로그인 상태는 `/api/auth/me` 로만 확인한다(쿠키는 HttpOnly 라 JS 가 읽지 않는다). 소셜 로그인 · 연결과 OAuth 동의는 서버로 가는 진짜 폼 POST 이고, 백엔드가 콜백 · OAuth 주소를 dev 서버 출처로 만들도록 프록시는 Host 를 바꾸지 않는다. 기본 SPA(Angular)가 아닌 SPA 로 띄우면 메일 링크 · issuer 출처를 맞춘다 — `App__PublicOrigin=https://localhost:61195 dotnet run … -p:SpaClient=sveltekit`.

화면 문구는 한국어(해요체)이고, 스타일은 의존성 없는 공통 CSS 하나다 — Angular `src/styles.css`, SvelteKit `src/app.css`, Nuxt `app/assets/css/app.css` 가 같은 내용이다(색은 `:root` 변수, 라이트 · 다크는 시스템 설정). 디자인을 바꾸면 세 파일을 함께 고친다.

### 4. 비밀값 출처를 고른다

애플리케이션은 비밀값을 **.NET 구성 키**(`ConnectionStrings:Default`)로만 읽는다. 어디서 채울지는 실행 프로파일(`ASPNETCORE_ENVIRONMENT`)이 고른다 — 「설정과 비밀값」 참고. 쓰지 않는 클라우드는 「쓰지 않는 클라우드 지우기」대로 지운다.

### 5. 지워도 되는 샘플

| 지울 것 | 함께 지워야 하는 것 |
|---|---|
| Server 의 `Weather/` 도메인 폴더 — `WeatherForecastController` · `WeatherForecastMappingService` · `WeatherForecastBizService` · `WeatherForecastDto` · `WeatherForecastModel` · `WeatherForecastMapper` | `FunctionalTests/WeatherForecastApiTests.cs`, 각 SPA 의 예제 페이지·`weather-forecast` 모듈과 테스트, SPA dev 서버 프록시의 `/weatherforecast` 경로 |
| Product 한 벌 — Common · Server · InternalApi 의 `Product/` 도메인 폴더(Common: `ProductStatus` · `Product*Dto` · `ProductStatusTransition` · `StaleDraftPolicy` · `ProductBizService` · `Persistence/` 의 엔티티 · 구성 · 매퍼 · EntityService, Server: `ProductController` · `ProductMappingService` · `Product*Model` · `ProductModelMapper`) (엔드포인트: GET · POST · PUT status · DELETE · GET status-history) | `AppDbContext.Products` · `ProductStatusHistories`, `Persistence/Migrations/` 를 지우고 마이그레이션을 새로 만든다. 테스트의 `ProductApiTests`(기능·통합) · `ProductBizServiceTests` · `ProductStatusTransitionTests` · `Support/InMemoryProductEntityService` · `ProductWebApplicationFactory` |
| Batch 의 `Product/Jobs/DiscontinueStaleDraftJob`, `Configuration/DiscontinueStaleDraftJobOptions` (+ Common 의 `Product/Domain/StaleDraftPolicy`, `ProductBizService.DiscontinueStaleDraftsAsync`, `ListStaleDraftsAsync`) | Program.cs 의 `jobs.Add<DiscontinueStaleDraftJob>()` 와 옵션 바인딩, `appsettings*.json` 의 `Batch:Jobs:DiscontinueStaleDraftJob`, 테스트의 `DiscontinueStaleDraftJobTests` · `StaleDraftPolicyTests` |
| Server 의 `Example/` 도메인 폴더 — `ExampleApiClient` · `ExampleApiOptions` · `ExampleStatusDto` | Program.cs 의 `AddApiClient<ExampleApiClient, …>` 줄, `appsettings.json` 의 `Clients:ExampleApi`, `UnitTests/Clients/ExampleApiClientTests.cs` |
| `AwsSecretSourceTests` (Aws 를 안 쓰면) | 통합 테스트 csproj 의 `AWSSDK.SecretsManager` · `Testcontainers.LocalStack` |

### 6. 뜨는지 확인한다

```bash
docker compose up -d                                    # SQL Server · Redis · Mailpit(http://localhost:8025) · mock OAuth(8090) — mssql-init 이 skeleton DB 를 만든다
dotnet tool restore                                     # dotnet-ef (dotnet-tools.json 에 버전 고정)
ASPNETCORE_ENVIRONMENT=Local dotnet ef database update \
  --project src/common/AspNetCoreSkeletonApplication.Common \
  --startup-project src/web-application/AspNetCoreSkeletonApplication.Server
dotnet build AspNetCoreSkeletonApplication.slnx
cd src/web-application/AspNetCoreSkeletonApplication.Server
dotnet run --launch-profile https                       # Local 프로파일 + SpaProxy
curl -k https://localhost:7274/health                   # Healthy
curl -k https://localhost:7274/api/products             # []
```

브라우저에는 Scalar(`/scalar`)가 열리고, SPA 는 dev 서버 주소(Angular 기본 https://localhost:61194)에서 뜬다.

## 구조

```
src/
  common/             Common   공유 라이브러리 — 영속 계층, 공유 BizService·Domain, Dto, 비밀값 출처 구성
  web-application/    Server   public 앱 — SPA 호스트, 컨트롤러, MappingService, 앱 전용 BizService
  internal-api/       InternalApi  게이트웨이 뒤 내부 API 앱 — SPA 없음 (템플릿 --internalApi)
  batch/              Batch        배치 앱 — Quartz.NET 스케줄 (템플릿 --batch)
  spa/                client.angular · client.sveltekit · client.nuxt
test/
  unit-tests/         UnitTests        외부 의존 없음
  functional-tests/   FunctionalTests  WebApplicationFactory 로 HTTP 호출, DB 불필요
  integration-tests/  IntegrationTests Docker 필요 (Testcontainers SQL Server · LocalStack)
  e2e-tests/          E2ETests         Docker · Node 필요 — SPA 를 빌드해 Playwright(Chromium)로 화면을 조작
```

**프로젝트는 계층이 아니라 공유·배포 단위로 나눈다.** 여러 앱이 함께 쓰는 것은 `Common`, 배포되는 앱은 각자 프로젝트다. 의존은 앱(`Server` · `InternalApi` · `Batch`) → `Common` 한 방향이고, 앱끼리는 서로 참조하지 않는다.

**프로젝트 안은 도메인 → 계층 순서로 나눈다.** 업무 코드의 네임스페이스는 `{프로젝트}.{도메인}.{계층}` 이다(예: `Common.Product.Persistence.Services`, `Server.Product.Controllers`). 같은 도메인 이름은 프로젝트를 건너 같은 도메인이다. 계층은 「요청 흐름」 참고.

| 영역 | 폴더 | 들어가는 것 |
|---|---|---|
| 업무 도메인 | `{도메인}/` 아래 `Controllers/` · `Services/Mapping/` · `Services/Biz/` · `Domain/` · `Models/` · `Dtos/` · `Enums/` · `Mappers/` · `Clients/` · `Jobs/` · `Persistence/{Entities,Configurations,Mappers,Services}/` | 그 도메인의 모든 계층. 예: `Product/`(Common · Server · InternalApi · Batch), `Mail/`(Common — 아웃박스 · SMTP 클라이언트), `Weather/` · `Example/`(Server), `JobTrigger/`(Batch 웹훅) |
| 공통 기반 (Common) | `Configuration/` · `Persistence/` · `Web/` · `Errors/` · `Contexts/` · `Constants/` · `Diagnostics/` · `Enums/` | DI 등록과 비밀값 출처, `AppDbContext` · 인터셉터 · 컨벤션 · 캐시 · 마이그레이션 · 엔티티 기반 타입(`Persistence/Entities/Backbone`), 웹 파이프라인 · 인증, 에러, 열거형 외부 코드(`EnumCodes`) |
| 공통 기반 (Batch) | `Configuration/` · `Scheduling/` · `Security/` | 스케줄러 구성, Job 기반 타입(`Scheduling/Jobs/TracedJob`), 웹훅 · 대시보드 인증 |

- 공통 기반 영역에는 계층의 **기반(abstract) 타입만** 둔다. 구체 업무 타입은 모두 도메인에 속한다.
- 도메인끼리는 **BizService · Dto 로만** 만난다. 다른 도메인의 엔티티 · EntityService 를 직접 쓰면 그 도메인의 캐시 무효화와 판단 규칙을 우회하기 때문이다.
- `InternalApi` 는 같은 공유 BizService 를 자기 Model 로 노출한다(예: 감사 주체 ID 까지 내보내는 `ProductModel`). `Batch` 의 Job 은 공유 BizService 를 부른다 — 「Batch」 참고.

**Entity · `AppDbContext` · Entity 매퍼는 `Common` 안에서 `internal` 이다.** 앱은 DbContext 를 볼 수 없고, EntityService 의 public 메서드에 Entity 를 쓰면 접근성 불일치(CS0050 · CS0051)로 컴파일이 깨진다 — 「Entity 는 EntityService 위로 올라가지 않는다」를 컴파일러가 지킨다.

**컴포지션 루트는 각 앱의 `Program.cs`** 다(`InternalApi` · `Batch` 는 top-level 문 대신 네임스페이스 안의 `Program` 클래스 — 테스트가 여러 앱을 함께 참조해도 이름이 겹치지 않는다). 비밀값 출처(`AddSecretSource<Program>()`), Common(`AddCommon()`), 앱의 계층 서비스(`AddLayeredServices(typeof(Program).Assembly)`) 순서로 붙인다. 계층 서비스는 이름 접미사로 찾아 scoped 로 등록하므로 서비스를 추가할 때 등록 코드를 쓰지 않는다 — `~MappingService` · `~BizService` 는 자기 타입으로, `~EntityService` 는 `I~EntityService` 로.

엔티티를 추가하면 `AppDbContext` 에 `DbSet` 을 두고 마이그레이션을 만든다.

```bash
dotnet tool restore
ASPNETCORE_ENVIRONMENT=Local dotnet ef migrations add <이름> \
  --project src/common/AspNetCoreSkeletonApplication.Common \
  --startup-project src/web-application/AspNetCoreSkeletonApplication.Server \
  --output-dir Persistence/Migrations
```

- `dotnet ef` 는 레포 로컬 도구다(`dotnet-tools.json`, EF Core 와 같은 버전). 시작 프로젝트를 띄워 구성을 읽으므로 연결 문자열이 있는 프로파일(`Local`)로 실행한다.
- `--namespace` 는 주지 않는다 — 주면 모델 스냅샷이 엉뚱한 폴더에 생긴다. `--output-dir` 만으로 네임스페이스가 `…Common.Persistence.Migrations` 가 된다.
- 마이그레이션은 앱 기동 시 자동 적용하지 않는다. 로컬은 `dotnet ef database update`, 배포는 마이그레이션 번들이나 스크립트(`dotnet ef migrations script --idempotent`)로 적용한다.

## 요청 흐름

`Controller → MappingService → BizService → EntityService → DbContext` 다. BizService 는 업무 판단을 순수 `Domain` 에 맡긴다. 계층을 건너뛰지 않고, **규칙에는 예외 케이스를 두지 않는다** — "이 경우엔 건너뛰어도 된다" 가 생기면 그 경우가 기본이 된다.

| 계층 (네임스페이스) | 하는 일 | 주입받는 것 |
|---|---|---|
| Controller (`.Controllers`) | HTTP 처리와 검증. 요청과 응답 모두 Model | MappingService, `IAuthorizationService`(리소스 기반 인가) |
| MappingService (`.Services.Mapping`) | 화면용 변환 전부 — 파생 값, 포맷, 여러 BizService 결과 조합. 트랜잭션 없음 | BizService, `IOptions<>`, `TimeProvider` |
| BizService (`.Services.Biz`) | 조회·저장·트랜잭션·호출 순서. 업무 판단은 Domain 을 불러 얻는다. 변경 메서드는 **항상** `IMainTransaction.ExecuteAsync` 안에서 돈다 | `I~EntityService`, `IMainTransaction`, `~Client`, 다른 BizService, `IOptions<>`, `TimeProvider` |
| Domain (`.Domain`) | 업무 값 계산과 상태 전이 판단 전부. EF 를 모르는 순수 클래스, DI 대상이 아니다 | — (Dto · 열거형 · 에러 · System 만 참조) |
| EntityService (`.Persistence.Services`) | Entity 하나의 CRUD. 밖으로는 Dto 만 주고받는다. 모두 `I~EntityService` 를 구현하고, 구현은 `internal` 이다. 조회는 기본 NoTracking, 바꿀 때만 `AsTracking()`. 캐시는 여기서만 | `AppDbContext`, `EntityCache`, OpenIddict 관리자(`IOpenIddictTokenManager` · `IOpenIddictAuthorizationManager` — OAuth 저장소는 OpenIddict 가 소유한다) |
| 매퍼 (`.Mappers`, `.Persistence.Mappers`) | 필드를 기계적으로 옮긴다. 계산하지 않는다. `static partial` (Mapperly) | — |

모든 계층은 `ILogger<>` 를 더 받을 수 있다. 이 표의 주입 허용 목록은 규약 테스트가 강제한다 — 「코딩 규약」 참고. 타입은 셋으로 나눈다 — **Model**(HTTP 입출력, `.Models`), **Dto**(계층 사이 전달, `.Dtos`), **Entity**(DB 매핑, `internal`). Model 과 Dto 는 데이터만 담는 `sealed record` 다.

메서드 이름은 한 벌이다 — `Get`(없으면 `ItemNotFoundException`), `Find`(없으면 null), `List`, `Create`, `Update`, `Delete`(비동기는 `~Async`).

요청 Model 의 검증 특성(`[Required]` · `[Range]` 등)은 record **생성자 매개변수**에 붙인다(`property:` 로 붙이면 MVC 가 요청을 500 으로 막는다). 값 타입 필수 필드는 빠지면 0 이 되므로 `[property: JsonRequired]` 를 함께 붙인다 — `ProductCreateModel` 참고.

예: `GET /weatherforecast` — `WeatherForecastController` 가 `WeatherForecastMappingService.List()` 를 부르고, MappingService 는 `WeatherForecastBizService` 가 만든 `WeatherForecastDto` 에 화씨(표시용 파생 값)를 계산해 `WeatherForecastMapper.ToModel(dto, temperatureF)` 로 `WeatherForecastModel` 을 만든다.

예: `PUT /api/products/{id}/status` — `ProductController` → `ProductMappingService` → 공유 `ProductBizService`(Common) 가 `IMainTransaction` 안에서 `IProductEntityService.GetAsync` 로 현재 상태를 읽고, `ProductStatusTransition.EnsureCanChange`(Domain) 로 판단을 얻은 뒤 `UpdateStatusAsync` 로 바꾼다. 허용되지 않은 전이는 Domain 이 `ConflictException`(409)을 던진다. 바뀌면 같은 트랜잭션에서 상태 이력(`GET /api/products/{id}/status-history`)을 남긴다.

### 외부 API 클라이언트

외부 API 는 앱의 `Clients/` 에 typed HttpClient `~Client` 로 두고 BizService 만 주입받는다. 등록은 Program.cs 한 줄이다.

```csharp
builder.Services.AddApiClient<ExampleApiClient, ExampleApiOptions>("Clients:ExampleApi");
```

이 한 줄이 붙이는 것 — `~Options`(`ApiClientOptions` 상속) 바인딩과 기동 시 검증(`BaseAddress` 가 없으면 기동이 멈춘다), 표준 resilience(재시도 · 타임아웃 · 회로 차단), 시도마다 `X-Trace-Id` 전파, 재시도 뒤에도 응답이 없으면 `ApiCallException`(502). 응답은 `ReadRequiredFromJsonAsync<T>()` 로 읽는다 — 오류 상태나 빈 본문도 `ApiCallException` 이 되어 실패가 null 로 새지 않는다.

## 캐시

EntityService 의 조회를 **인스턴스 메모리(L1) + Redis(L2) + Redis 백플레인**으로 캐시한다. 구현은 FusionCache 이고, 코드는 `HybridCache` 추상화로만 쓴다(`AsHybridCache()`). 연결 문자열은 비밀값 `ConnectionStrings:Redis`, 로컬은 docker compose 의 Redis 다.

**여러 인스턴스에서도 무효화가 곧바로 닿는다.** 한 인스턴스가 키나 태그를 지우면 백플레인(Redis pub/sub)이 다른 모든 인스턴스 — 같은 `KeyPrefix` 를 쓰는 Server · InternalApi · Batch 포함 — 의 L1 에서도 지운다. 그래서 L1 만료를 따로 짧게 두지 않는다.

규칙 (예외 없음):

- **캐시는 EntityService 에서만**, `HybridCache` 를 직접 쓰지 않고 `EntityCache` 를 거친다(주입 허용 목록이 강제한다).
- 키는 `{엔티티}:{id}` · `{엔티티}:list` · 조건이 붙은 목록은 `{엔티티}:list:{조건}`(`user_group_member:list:user:7`), 태그는 `{엔티티}` — 엔티티 이름은 테이블 이름과 같다. **캐시하는 엔티티의 모든 변경 메서드는 그 태그 전체를 무효화한다** — 키를 골라 지우다 빠뜨리는 일이 없다(형제 스켈레톤의 `ProductIdList` 처럼 무효화가 없는 캐시가 생기지 않는다).
- **없는 값은 캐시하지 않는다.** `EntityCache.FindOrLoadAsync` 가 null 결과를 캐시에 넣지 않는다.
- **무효화는 커밋 뒤에.** 트랜잭션 안에서 부른 무효화는 모아 두었다가 `IMainTransaction` 이 커밋한 직후에 실행하고, 롤백되면 버린다. 커밋 전에 지우면 그 사이 다른 요청이 옛 값을 다시 캐시해 만료까지 남는다.

| 설정 (`Cache:*`) | 기본 | 뜻 |
|---|---|---|
| `Expiration` | 10분 | L1 · Redis 공통 만료 |
| `KeyPrefix` | `asp-net-core-skeleton-application:` | Redis 키 접두사. 캐시 항목은 `{KeyPrefix}cache:` 아래에 둔다. 같은 Redis 를 여러 서비스가 쓸 때 섞이지 않게 한다 |

- 백플레인 메시지는 비동기라 다른 인스턴스에 닿기까지 수 ms 가 걸린다. 같은 요청 안에서 쓰고 곧바로 다른 인스턴스로 읽는 흐름은 없게 설계한다.
- Redis 가 잠시 끊겨도 요청은 실패하지 않는다 — FusionCache 가 L2 · 백플레인 오류를 기록하고 DB 에서 읽는다. 다시 연결되면 백플레인이 자동으로 이어진다.

## 에러 응답

모든 에러 응답은 **ProblemDetails**(`application/problem+json`, RFC 9457)다. 어디서 만들어졌든 확장 필드 `code` · `divisionCode` · `traceId` 가 붙는다.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "상품 42 가 없다",
  "code": "NOT_FOUND_ERROR",
  "divisionCode": "G009",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

| 응답을 만든 곳 | 상태 · 코드 | `detail` |
|---|---|---|
| `ItemNotFoundException` | 404 `NOT_FOUND_ERROR` (G009) | 예외 메시지 |
| `ClientBadRequestException` | 400 `BAD_REQUEST_ERROR` (G001) | 예외 메시지 |
| `ConflictException` | 409 `CONFLICT_ERROR` (G013) | 예외 메시지 |
| 로그인 — 계정 잠김 | 423 `ACCOUNT_LOCKED_ERROR` (AUTH001) | 안내 문구 |
| 로그인 — 이메일을 확인하지 않은 계정(비밀번호가 맞을 때만) | 403 `EMAIL_NOT_CONFIRMED_ERROR` (AUTH006) | 안내 문구 |
| `ApiCallException` (외부 API 실패) | 502 `EXTERNAL_API_ERROR` (G014) | 없음 — 로그에만 남는다 |
| DataAnnotations 검증 실패 (`[ApiController]` 자동 400) | 400 `NOT_VALID_ERROR` (G011), `errors` 에 필드별 메시지 | — |
| 컨트롤러가 돌려준 상태 코드(`NotFound()` 등) | 상태 코드로 고른 코드 | — |
| 본문 없는 응답 — 인증 실패 401 · 인가 실패 403 · 없는 경로 등 (`UseStatusCodePages`) | 401 `UNAUTHORIZED_ERROR` (G015) · 403 `FORBIDDEN_ERROR` (G008) · 상태 코드로 고른 코드 | — |
| 그 밖의 예외 | 500 `INTERNAL_SERVER_ERROR` (G999) | 없음 — 내부 메시지를 내보내지 않는다 |

- 업무 실패는 `CodedException` 계열을 던진다. 메시지는 4xx 응답의 `detail` 로 그대로 나가므로 사용자에게 보여도 되는 문장으로 쓴다. 예외를 잡아서 삼키지 않는다.
- 코드는 `Common/Errors/ErrorCode` 의 정적 필드다. 업무 코드가 필요하면 정적 필드를 더 두고 `CodedException` 을 상속한다.
- 처리는 `Common/Web` 의 `GlobalExceptionHandler`(예외 → ProblemDetails)와 `AddProblemDetails` 의 `CustomizeProblemDetails`(예외가 아닌 응답에 코드를 붙임) 두 곳이다. 앱은 `AddSkeletonWeb()` · `UseSkeletonWeb()` 로 붙인다.
- 코드 문자열과 구분 코드는 Spring 형제 스켈레톤과 같지만 **응답 형식은 다르다** — 형제는 자체 `ErrorResponseModel` 을, 이 스켈레톤은 .NET 표준 ProblemDetails 를 쓴다.

## 추적 ID 와 감사

### 추적 ID — `X-Trace-Id`

요청마다 추적 ID 하나를 정해 로그와 응답, 나가는 호출을 한 줄로 잇는다. 헤더 이름과 검증 규칙은 Spring 형제 스켈레톤과 같다.

1. 받은 `X-Trace-Id` 가 `^[A-Za-z0-9._-]{1,64}$` 이면 그 값을 쓴다. 없거나 어긋나면(개행 주입 등) 버리고 새로 만든다 — 현재 W3C trace-id, 없으면 GUID.
2. 그 값을 `TraceIdContext.Current`(AsyncLocal), 로깅 scope `X-Trace-Id:{XTraceId}`, `HttpContext.TraceIdentifier`, 응답 헤더 `X-Trace-Id`, 에러 응답의 `traceId` 에 싣는다. scope 키를 `XTraceId` 로 둔 것은 ASP.NET Core 가 넣는 W3C `TraceId` scope 와 구분하려는 것이다 — 헤더 없이 들어온 요청은 두 값이 같다.

`Common/Web/TraceIdMiddleware` 가 하고, `UseSkeletonWeb()` 이 파이프라인 맨 앞에 둔다. 콘솔 로그에 scope 를 찍는 설정은 `appsettings.json` 의 `Logging:Console` 이다(`FormatterName: simple` 과 `FormatterOptions:IncludeScopes: true` — 이름을 적지 않으면 옵션이 적용되지 않는다). AsyncLocal 이라 `await` · `Task.Run` 을 따라 흐르므로 Spring 의 `MdcTaskDecorator` 같은 전달 장치는 필요 없다.

### 느린 쿼리 · 요청 경고

임계값을 넘으면 경고 로그를 남긴다. 추적 ID 는 scope 로 함께 찍히므로 같은 요청의 느린 쿼리와 느린 요청을 이어 볼 수 있다.

| 대상 | 임계값 (`appsettings.json`) | 남기는 것 |
|---|---|---|
| DB 명령 하나 — `SlowQueryInterceptor` | `Diagnostics:SlowQueryThreshold` (기본 500ms) | 실행 시간, SQL(매개변수 값은 남지 않는다, 1000자에서 자름) |
| HTTP 요청 하나 — `SlowRequestMiddleware` | `Diagnostics:SlowRequestThreshold` (기본 1s) | 메서드, 경로(쿼리 문자열 제외), 상태 코드, 처리 시간 |

형제 스켈레톤의 `@SlowMethod`(AOP)는 두 레포 모두 적용된 곳이 없었다. 여기서는 메서드마다 붙이지 않고 경계(DB 명령 · HTTP 요청) 두 곳에서 모두 잰다. 특정 구간을 따로 재야 하면 `ActivitySource` span 이나 `Stopwatch` 를 그 자리에 둔다.

### 감사 — 누가 언제 만들고 고쳤나

감사 필드는 `AuditSaveChangesInterceptor` 가 저장 직전에 채운다(시각은 `TimeProvider` 의 UTC). EntityService 나 매퍼가 직접 넣지 않는다. 엔티티의 기반은 행의 성격으로 고른다.

| 기반 | 필드 | 쓰는 곳 |
|---|---|---|
| `BaseAuditedEntity` | `CreatedBy` · `CreatedAt` · `UpdatedBy` · `UpdatedAt` | 고칠 수 있는 업무 행(`ProductEntity`). 수정 때 생성 필드는 바뀌지 않는다 |
| `BaseCreateAuditedEntity` | `CreatedBy` · `CreatedAt` | 한 번 쓰고 바꾸지 않는 이력 · 로그(`ProductStatusHistoryEntity`). **추가 전용** — 수정 · 삭제하면 저장할 때 예외 |
| `ISoftDeletable` (함께 구현) | `DeletedAt` · `DeletedBy` | 지워도 남겨야 하는 업무 행(`ProductEntity`). EntityService 는 `Remove` 를 부르고 인터셉터가 논리 삭제로 바꾼다. 지워진 행은 전역 쿼리 필터가 모든 조회에서 뺀다(꼭 봐야 하면 그 쿼리에서만 `IgnoreQueryFilters()`) |

감사 주체는 `AuditorContext` 하나만 읽고, HTTP 요청에서는 `AuditorContextMiddleware` 가 다음 순서로 정한다.

| 순위 | 출처 | 조건 |
|---|---|---|
| 1 | 인증 주체 — `NameIdentifier` 또는 `sub` claim | 인증이 붙어 있을 때 |
| 2 | `X-User-Id` 헤더 | `Audit:TrustUserHeader=true` 일 때만 (기본 false) |
| 3 | `AuditorIds.System` (`0`) | 위가 없을 때 — 시스템 자신이거나 주체를 모른다는 뜻 |

값은 `long.TryParse` 로만 읽는다 — 빈 값 · 숫자 아님 · 범위 초과는 버린다(자르면 남의 ID 가 된다). **`X-User-Id` 는 외부 입력이다.** 그대로 믿으면 호출자가 다른 사용자로 감사 기록을 위조할 수 있으므로, 게이트웨이가 인증 결과로 헤더를 덮어쓰고 밖에서 온 같은 이름의 헤더를 지우는 배포에서만 켠다. 내부 API 는 인증이 없어 헤더가 유일한 출처다 — 게이트웨이 뒤에 둘 때 `Audit__TrustUserHeader=true` 로 켠다. 미들웨어는 인증 주체를 읽으므로 `UseAuthorization()` 뒤에 `UseAuditorContext()` 로 붙인다. 배치처럼 HTTP 가 아닌 실행은 진입점에서 `AuditorContext.Begin(userId)` 로 채운다.

## Batch

`Batch` 앱은 **Quartz.NET 4.5** 로 Job 을 스케줄한다. 실행 이력 · 재시도 · 동시 실행 방지는 Quartz 가 맡고, 업무 처리는 Job 이 공유 BizService 를 불러서 한다 — Spring Batch 같은 chunk 프레임워크는 두지 않는다(.NET 에 유지되는 대응이 없다).

**스케줄러** — `Configuration/BatchSchedulerExtensions.AddBatchScheduler`

- SQL Server 영속 저장소(`quartz` 스키마, 테이블 접두사 `quartz.QRTZ_`) · **클러스터** · 실행 이력(`QRTZ_EXECUTION_HISTORY`). 인스턴스가 여러 개 떠도 한 Job 은 클러스터 전체에서 한 번만 돈다. 노드 ID 는 기동할 때 생성하고, 스케줄러 이름(`SCHED_NAME`)은 앱 이름이다.
- 스키마는 Common 마이그레이션 `AddQuartzScheduler` 가 만든다 — Quartz 4.5.0 의 정본 DDL(`Persistence/Migrations/Scripts/quartz-4.5.0-sqlserver.sql`)을 그대로 실행한다. 프레임워크 정본이라 업무 테이블 규칙(FOREIGN KEY 금지 등)의 대상이 아니다. Quartz 를 올리면 새 버전 스크립트로 새 마이그레이션을 만든다.
- 스케줄은 `Batch:Jobs:{Job 클래스 이름}` 구성에서만 정한다(cron 을 코드에 적지 않는다). `Enabled=false` 면 트리거를 두지 않고 — 저장소에 남은 옛 트리거도 기동할 때 지운다 — Job 은 남아 수동 실행은 된다. 켜졌는데 `Cron` 이 없거나 섹션이 없으면 기동이 멈춘다.

**Job 규칙** (예외 없음, `JobConventionTests` · `LayeringConventionTests` 가 강제)

- 도메인의 `Jobs/`(`{도메인}.Jobs`) 에 두는 `~Job` 이고 `TracedJob`(`Scheduling/Jobs/`) 을 상속한다. `TracedJob` 이 실행마다 추적 ID(수동 실행이 넘긴 `traceId` 를 검증해 쓰거나 새로), 감사 주체(언제나 시스템 `0`), 로깅 scope(`X-Trace-Id` · `Job`)와 시작 · 끝 · 실패 로그를 정한다. 실패는 다시 던져 Quartz 실행 이력에 실패로 남긴다.
- `[DisallowConcurrentExecution]` 을 단다 — 클러스터 전체에서 같은 Job 은 한 번에 하나만 돈다.
- 주입받는 것은 BizService · `IOptions<>` · `TimeProvider` · `ILogger<>` 뿐이다. 업무 판단은 BizService 가 Domain 으로, 쓰기는 BizService 의 `IMainTransaction` 안에서 한다.

**샘플 — `DiscontinueStaleDraftJob`** (매일 02:00, 로컬은 5분마다)

`StaleAfterDays`(기본 30)일 넘게 손대지 않은 Draft 상품을 판매 종료한다. Job 은 현재 시각과 구성을 넘길 뿐이고, 공유 `ProductBizService.DiscontinueStaleDraftsAsync` 가 다음을 한다.

1. 기준 시각은 Domain(`StaleDraftPolicy.CutoffOf`)이 정한다.
2. 대상은 **keyset 페이지**(`Id > 마지막 id`, id 순)로 읽는다 — 처리하면서 대상이 줄어도 건너뛰는 행이 없다.
3. 페이지마다 `IMainTransaction` 하나로, 웹의 상태 변경과 **같은 경로**(Domain 판단 → 변경 → 상태 이력)로 바꾼다. 감사 주체는 시스템(`0`), 캐시 무효화는 커밋 뒤.

**멱등** — 처리한 상품은 조회 조건(Draft)에서 빠지므로 다시 실행하면 대상이 0건이다. `DiscontinueStaleDraftJobTests` 가 Quartz 로 두 번 실행해 1회차 결과 · 2회차 0건 · 데이터 스냅샷 동일 · 실행 이력 성공 2건을 확인한다. 새 Job 도 같은 모양으로 — 처리 완료 상태를 데이터에 남기고, 조회 조건이 그것을 걸러야 한다.

**`PruneOAuthTokenJob`** (매일 03:30) — OAuth 서버가 쌓은 토큰 · 동의 중 더 쓸 수 없는 것(만료 · 폐기 · 사용됨, 토큰 없는 일회성 동의)을 지운다. 만들어진 지 `Batch:Jobs:PruneOAuthTokenJob:RetentionDays`(14일) 지난 것만 — 지울지는 OpenIddict 의 정리 규칙을 그대로 쓴다(`OAuthTokenBizService` → `IOAuthTokenEntityService`). 지운 것은 대상에서 빠지므로 다시 실행해도 같다(`PruneOAuthTokenJobTests`). Batch 를 빼고 만든 템플릿(`--batch false`)에는 이 정리가 없다 — 토큰 테이블이 계속 자라므로 다른 스케줄러에서 `OAuthTokenBizService.PruneAsync` 를 부른다.

**수동 실행 — 웹훅** `POST /api/jobs/{Job 이름}/trigger`

```bash
curl -X POST https://localhost:7278/api/jobs/DiscontinueStaleDraftJob/trigger \
  -H 'X-Api-Key: <키>' -H 'Content-Type: application/json' -d '{"parameters": {}}'
# 202 {"jobName":"DiscontinueStaleDraftJob","traceId":"…"}
```

| 순서 | 검사 | 실패 |
|---|---|---|
| 1 | `X-Api-Key` 가 `Batch:Webhook:ApiKeys` 의 키와 맞는가(상수 시간 비교) | 401 `UNAUTHORIZED_ERROR` |
| 2 | 그 키의 `AllowedJobs` 에 경로의 Job 이 있는가 — 없는 Job 이름도 같은 403(존재를 드러내지 않는다) | 403 `FORBIDDEN_ERROR` |
| 3 | 본문 `parameters` 의 이름이 그 Job 의 `Batch:Jobs:{Job}:AllowedParameters` 에 있는가 — 거부 응답에는 이름만, 값은 담지 않는다 | 400 `BAD_REQUEST_ERROR` |
| 4 | Job 이 스케줄러에 있는가 | 404 `NOT_FOUND_ERROR` |

통과하면 Quartz `TriggerJob` 으로 실행하고 202 와 요청의 추적 ID 를 돌려준다 — Job 로그와 실행 이력(수동 실행으로 표시)을 이 ID 로 찾는다. 스케줄 실행과 같은 Quartz 경로라 `[DisallowConcurrentExecution]` 이 웹훅에도 걸린다 — 형제 스켈레톤처럼 웹훅과 스케줄이 같은 Job 을 동시에 돌리는 일이 없다.

API 키는 이름 · 키 · 허용 Job 으로 적고, **키는 비밀값**이다. 기동할 때 검증한다 — 이름 없음, 32자보다 짧은 키, 같은 이름 중복은 기동 실패. 허용 Job 이 비어 있는 키는 아무것도 실행하지 못한다(fail-closed).

```bash
# 로컬 — user-secrets (Batch 프로젝트에서)
dotnet user-secrets set "Batch:Webhook:ApiKeys:0:Name" "local"
dotnet user-secrets set "Batch:Webhook:ApiKeys:0:Key" "$(openssl rand -hex 32)"
dotnet user-secrets set "Batch:Webhook:ApiKeys:0:AllowedJobs:0" "DiscontinueStaleDraftJob"
# 배포 — 환경변수 Batch__Webhook__ApiKeys__0__Key, 비밀 저장소 이름 Batch--Webhook--ApiKeys--0--Key
```

**운영 대시보드** `/quartz` — Quartz.NET 내장 대시보드(Blazor). Job · 트리거 · 실행 중 · 실행 이력(클러스터 전체, DB 의 `QRTZ_EXECUTION_HISTORY`)을 보고, 멈춤 · 재개 · 지금 실행을 할 수 있다.

- **운영자 정책 뒤에 있다** — 브라우저용 Basic 인증, 계정은 `Batch:Dashboard:Username` · `Password`(비밀값, 16자 이상). 계정을 적지 않으면 아무도 들어오지 못한다. 대시보드의 Blazor 스크립트 · CSS 같은 정적 자산도 같은 정책 뒤다. 웹훅 API 키로는 열리지 않는다.
- **다룰 수 있는 Job 타입은 이 앱의 `Jobs` 네임스페이스뿐이다**(`IsJobTypeAllowed`) — 대시보드로 임의 타입(예: 실행 파일을 띄우는 `NativeJob`)의 Job 을 추가하는 길을 막는다.
- Basic 인증은 평문 계정을 매 요청에 싣는다 — HTTPS 로만 연다(Batch 는 HTTPS 로 리다이렉트한다). 사내망 · VPN 뒤에 두는 것을 권한다.

```bash
dotnet user-secrets set "Batch:Dashboard:Username" "operator"   # Batch 프로젝트에서
dotnet user-secrets set "Batch:Dashboard:Password" "$(openssl rand -hex 16)"
# 배포 — Batch__Dashboard__Password, 비밀 저장소 이름 Batch--Dashboard--Password
```

**새 Job 체크리스트**

1. `{도메인}/Jobs/{이름}Job.cs` — `TracedJob` 상속, `[DisallowConcurrentExecution]`, `ExecuteJobAsync` 에서 BizService 호출
2. `Program.cs` 의 `AddBatchScheduler(..., jobs => jobs.Add<{이름}Job>())`
3. `appsettings.json` 의 `Batch:Jobs:{이름}Job` — `Enabled` · `Cron`
4. 재실행해도 결과가 같은지(멱등) 확인하는 통합 테스트 — `DiscontinueStaleDraftJobTests` 를 본뜬다
5. Job 고유 설정이 있으면 `Configuration/{이름}JobOptions` 를 같은 구성 섹션에 바인딩한다(`DiscontinueStaleDraftJobOptions` 참고)

## 인증

Server 는 **ASP.NET Core Identity + 쿠키**로 로그인한다 — 같은 출처 SPA 가 호출하는 앱이라 토큰을 JS 에 두지 않는다. Spring Security 의 자리는 ASP.NET Core 의 인증 · 인가 미들웨어가, `UserDetailsService` · `PasswordEncoder` · 사용자 테이블 · 계정 잠금은 Identity 가 맡는다. InternalApi 는 인증 없이 게이트웨이 헤더(`X-User-Id`)로 감사 주체만 받고, Batch 는 웹훅 API 키 · 대시보드 계정을 따로 쓴다.

| 엔드포인트 (`Common/Web/AuthenticationEndpoints`) | 동작 |
|---|---|
| `POST /api/auth/register` `{email, password}` | 201 — 로그인하지 않고 이메일 확인 링크를 메일로 보낸다. 비밀번호 10자 이상 · 대소문자 · 숫자 · 기호(Identity 기본). 어기면 400 `NOT_VALID_ERROR` |
| `POST /api/auth/email/confirm` `{userId, code}` | 204 — 메일 링크(`{App:PublicOrigin}/confirm-email?userId=…&code=…`)의 값으로 이메일을 확인한다. 로그인시키지 않는다. 토큰이 틀렸거나 만료(2시간)됐으면 400 `INVALID_TOKEN_ERROR` |
| `POST /api/auth/email/resend` `{email}` | 202 — 계정이 있든 없든, 이미 확인했든 같다. 확인 전 계정이면 링크를 다시 보낸다 |
| `POST /api/auth/login` `{email, password}` | 204 + 쿠키. 틀리면 401 — 이메일이 있는지 드러내지 않는다. **5번 틀리면 30분 잠기고 423 `ACCOUNT_LOCKED_ERROR`**(형제 스켈레톤과 같은 값 · 코드). 이메일을 확인하지 않은 계정은 비밀번호가 맞을 때만 403 `EMAIL_NOT_CONFIRMED_ERROR`. 2단계 인증을 켠 계정은 202 `{providers}` — 세션 쿠키는 두 번째 단계가 준다(기억한 브라우저면 바로 204) |
| `POST /api/auth/login/2fa` `{provider, code, rememberBrowser}` | 2단계 인증의 두 번째 단계 — `provider` 는 `authenticator` · `email` · `recovery`. 204 + 쿠키, 틀리면 401(잠금 횟수를 센다), 기다리는 로그인이 없으면 401. `GET` 은 기다리는 로그인의 수단 `{providers}`, `POST /email` 은 메일 코드를 보낸다(202) |
| `POST /api/auth/passkey/options` → `POST /api/auth/passkey/login` `{credential}` | 패스키 로그인 — 옵션은 Identity 가 만든 WebAuthn 요청 옵션 JSON 그대로, `credential` 은 브라우저 세리머니 결과의 JSON 문자열. 204 + 쿠키, 실패 401, 이메일 미확인 403, 잠김 423 |
| `/api/account/passkeys` (세션 쿠키) | `GET` 목록 `[{id, name, createdAt}]`, `POST /options`(등록용 생성 옵션) → `POST {credential, name}`(201, 확인 실패 400), `PUT /{id} {name}`, `DELETE /{id}`(마지막 로그인 수단이면 409) |
| `/api/account/two-factor` (세션 쿠키) | `GET` 상태 `{enabled, method, recoveryCodesLeft, browserRemembered}`. `POST /authenticator/setup`(새 키 · `otpauth://` URI · QR data URI) → `/authenticator/verify {code}`(켜고 복구 코드), `/email/enable`(메일 코드로 켜고 복구 코드), `/recovery-codes`(새로 받기), `/disable {password}`, `/forget-browser`. 이미 켜져 있는데 켜면 409 |
| `POST /api/auth/logout` | 204, 쿠키 삭제 |
| `POST /api/auth/password/forgot` `{email}` | 202 — 계정이 있든 없든 같다(가입 여부를 드러내지 않는다). 있으면 재설정 링크 메일을 보낸다 |
| `POST /api/auth/password/reset` `{email, code, newPassword}` | 204. 토큰이 틀렸거나 만료 · 사용됐으면 400 `INVALID_TOKEN_ERROR`(AUTH002), 비밀번호 규칙 위반은 400 `NOT_VALID_ERROR` |
| `GET /api/auth/me` | `{id, email, permissions}` — 실효 권한 코드(화면이 쓸 수 있는 기능을 고를 때 쓴다). 로그인하지 않았으면 401 |

- **이메일을 확인해야 로그인한다**(`SignIn.RequireConfirmedEmail`) — 비밀번호 · 소셜 로그인 모두. Identity 는 비밀번호를 보기 전에 확인 여부로 거절하므로(계정이 있다는 것이 드러나고 실패 횟수도 세지 않는다), 로그인 엔드포인트가 확인 전 계정만 먼저 비밀번호를 확인한다 — 틀리면 다른 계정과 같은 401 이고 잠금 횟수도 센다. 이 기능 전에 가입한 계정은 마이그레이션 `ConfirmExistingEmails` 가 확인된 것으로 둔다. SPA 는 가입 뒤 확인 메일을 안내하고, 로그인 화면에서 "확인 메일 다시 받기" 를, `/confirm-email` 화면에서 확인을 한다.
- **2단계 인증** (`Common/Web/TwoFactorEndpoints`, Identity 기본 기능) — 켠 계정은 비밀번호(또는 소셜 로그인) 다음에 확인 코드를 한 번 더 넣는다. 수단은 계정마다 하나다: 인증 앱(TOTP)을 등록했으면 인증 앱, 아니면 메일 코드(아웃박스로 보낸다). 인증 앱을 고른 계정에 메일 코드를 열어 두면 메일 계정 하나로 두 단계를 다 넘기 때문이다. 복구 코드 10개는 켤 때 한 번만 보여 주고 각각 한 번만 쓰인다. "이 브라우저 기억하기" 를 고르면 그 브라우저는 다음부터 묻지 않는다(`__Host-tfa-remember`). 틀린 코드도 잠금 횟수를 센다.
  - 켜기(인증 앱 등록 · 메일 코드)는 꺼져 있을 때만 된다 — 등록하다 만 인증 앱 키가 로그인 수단이 되지 않게. 수단을 바꾸려면 끈 뒤에 다시 켠다. 끄려면 비밀번호를 다시 넣는다.
  - 인증 앱 QR 코드는 서버가 SVG(QRCoder)로 그려 data URI 로 준다 — SPA 마다 QR 라이브러리를 두지 않는다.
  - 두 번째 단계를 기다리는 사용자(`__Host-tfa`)와 기억한 브라우저 쿠키는 SameSite=Lax 다. 소셜 로그인 콜백은 공급자에서 돌아오는 cross-site 이동이라 Strict 면 기억한 브라우저 쿠키가 실리지 않는다. 소셜 로그인 뒤 두 번째 단계가 필요하면 `/login?step=two-factor` 로 보낸다.
  - 관리 API(`/api/account/two-factor`)는 세션 쿠키로만 부른다 — Bearer 토큰으로는 계정 보안을 바꾸지 못한다.
  - 토큰을 고쳐 쓰는 저장(복구 코드 · 인증 앱 키)은 `ApplicationUserStore` 가 추적 조회로 한다 — `AppDbContext` 의 NoTracking 기본값에서 Identity 기본 저장소는 값만 바꾼 토큰을 저장하지 못한다.
- **패스키** (`Common/Web/PasskeyEndpoints`, .NET 10 Identity 내장) — 비밀번호 없는 로그인이다. 패스키는 그 자체로 다중 요소(기기 + 생체 · PIN)라 2단계 인증을 건너뛴다. 2단계 인증의 두 번째 수단으로는 쓰지 않는다(Identity 가 지원하지 않는다).
  - RP ID 는 `App:PublicOrigin` 의 호스트다 — 요청의 Host(리버스 프록시 뒤에서는 내부 이름일 수 있다)로 정하지 않는다. 세리머니의 출처는 요청의 Origin 과 같아야 한다.
  - 저장은 `identity_user_passkey`(Identity 스키마 3판, 마이그레이션 `AddPasskeys`). 판은 `AddCommon` 이 정한다(`AppDbContext.ConfigureIdentityStore`) — Identity 를 쓰지 않는 앱(Batch · InternalApi)과 앱 서비스 없이 만든 컨텍스트(테스트 · 도구)도 같은 모델을 만들게. 키 칼럼 길이는 1판 그대로(450) 둔다.
  - SPA 는 `webauthn` 모듈 하나로 세리머니를 한다 — 브라우저의 `parse*FromJSON` · `toJSON` 을 쓰고, 없거나 응답을 빠뜨리면 base64url 을 직접 바꾼다. 로그인 화면의 "패스키로 로그인", 내 계정의 「패스키」 절(등록 · 지우기). 사용자가 창을 닫으면 취소로 안내한다.
  - 브라우저 테스트는 Playwright 의 가상 인증기(`BrowserContext.Credentials`)로 등록 → 로그아웃 → 패스키 로그인을 확인한다.
- **쿠키** — `__Host-session`, HttpOnly · Secure · SameSite=Strict, 8시간(쓰면 연장). 로그인 페이지가 없으므로 인증이 필요하면 리다이렉트 대신 401, 권한이 없으면 403 을 ProblemDetails 로 돌려준다.
- **여러 인스턴스** — 쿠키를 암호화하는 Data Protection 키를 Redis 에 둔다(`{Cache:KeyPrefix}data-protection-keys`). 어느 인스턴스가 만든 쿠키든 다른 인스턴스가 읽는다. 키는 Redis 에 평문으로 있으므로 Redis 접근을 앱으로 제한한다.
- **감사** — 로그인한 요청은 사용자 ID(`NameIdentifier` claim)가 감사 주체가 된다(「추적 ID 와 감사」의 1순위).
- **계정 테이블** — Identity 정본 스키마를 `identity_` 접두사로 둔다(`identity_user`, `identity_user_login`, `identity_user_passkey` …). 역할 테이블은 없다 — 역할은 인가 도메인이 맡는다. 프레임워크 소유라 업무 테이블 규칙(FOREIGN KEY 금지 · `BaseEntity`)의 대상이 아니다. 업무 데이터는 이 테이블에 칼럼을 더하지 않고, 사용자 ID 로 가리키는 별도 엔티티에 둔다.

**인가** (예외 없음) — **사용자 → 그룹 → 역할 → 권한 코드**

- **기본은 로그인 필요다**(fallback 정책). 익명으로 열 엔드포인트는 하나씩 연다 — 컨트롤러 · 액션의 `[AllowAnonymous]`, 엔드포인트의 `.AllowAnonymous()`. Server 에서 열린 것: WeatherForecast, Product 조회(목록 · 단건 · 상태 이력), `/health`, OpenAPI · Scalar, SPA 정적 파일 · fallback, `/api/auth` 의 가입 · 로그인 · 이메일 확인 · 비밀번호 재설정.
- **권한 코드로 선언한다** — `[HasPermission(Permissions.Products.Manage)]`. 코드는 `Common/Authorization/Permissions` 카탈로그 하나가 정본이고(`{대상}.{동작}` 소문자), 카탈로그 밖 코드와 역할 문자열(`Roles = "…"`)은 규약 테스트가 막는다(`AuthorizationConventionTests`). 지금 코드: `products.manage`(상품 상태 변경 · 남의 상품 삭제), `authorization.manage`(그룹 · 역할 · 권한 관리).
- **사용자에게 역할을 직접 주지 않는다.** 사용자는 그룹(`user_group_member`)에 속하고, 그룹이 역할(`user_group_role`)을, 역할이 권한 코드(`role_permission`)를 갖는다. 실효 권한은 그 합집합이다(`EffectivePermissions`).
- **판정은 요청마다 서버가 한다** — 쿠키에 굳은 클레임을 쓰지 않는다. 조회 셋(사용자의 그룹, 그룹-역할, 역할-권한)은 캐시되고, 바뀌면 태그 무효화가 백플레인으로 모든 인스턴스에 곧바로 닿는다 — 권한을 빼면 다음 요청부터 403 이다.
- **판정의 입구는 `IPermissionEvaluator` 하나다.** 권한 정책 핸들러와 리소스 핸들러가 이것만 부른다. 그룹 · 역할 · 권한의 변경은 인가 도메인의 BizService(`UserGroupBizService` · `RoleBizService`)에서만 일어난다(도메인 경계 규약). 그래서 판정 방식을 ReBAC(OpenFGA 등)으로 바꿀 때 바꿀 곳은 평가기 구현과 이 두 BizService 의 관계 동기화다. 권한으로 목록을 거르는 조회는 두지 않는다 — 필요해지면 평가기에 목록 판정을 더해 그것만 쓴다.
- **데이터에 따라 갈리는 판정은 리소스 기반 인가로** — 컨트롤러가 리소스를 읽은 뒤 `IAuthorizationService.AuthorizeAsync(User, product, ProductOperations.Delete)` 를 부르고, 핸들러(`ProductAuthorizationHandler`)가 권한은 평가기에, 규칙은 순수 Domain(`ProductAccessRule`)에 묻는다. 샘플 규칙: 상품 관리 권한이 있거나, 아직 Draft 인 자기 상품이면 지울 수 있다.
- **관리 API** (`authorization.manage`) — `/api/authorization/groups`(CRUD, `/{id}/members/{userId}` · `/{id}/roles/{roleId}` 는 PUT 추가 · DELETE 제거, 멱등), `/api/authorization/roles`(CRUD, 권한은 집합째 보낸다), `/api/authorization/permissions`(카탈로그). **마지막 `authorization.manage` 보유자를 없애는 변경은 409** 로 되돌린다 — 운영자가 스스로를 잠그지 않는다. 카탈로그 밖 코드는 400.
- **시드** — 마이그레이션 `AddAuthorizationGroups` 가 그룹 `Administrators` 와 역할 `Administrator`(모든 권한)를 만든다. 첫 관리자는 가입한 뒤 SQL 로 그룹에 넣는다(아래). 그다음부터는 관리 API 로 한다.

**소셜 로그인** (Google · Kakao · Naver, `Common/Web/ExternalLoginEndpoints`)

- **연결된 계정만 로그인한다 — 자동 가입은 없다.** 연결은 로그인한 사용자가 계정 화면에서 한다. 공급자 이메일이 같다고 기존 계정에 붙이지 않는다(공급자 이메일 검증 여부에 기대지 않는다).
- **공급자는 구성으로 켠다** — `Authentication:External:{Google|Kakao|Naver}` 섹션이 있는 것만. 섹션이 있으면 `ClientId` · `ClientSecret`(비밀값)은 필수다. 나머지 키는 핸들러 옵션에 그대로 묶인다(로컬은 엔드포인트 주소 · scope 를 mock 서버에 맞춘다). 공급자 콘솔에 등록할 redirect URI 는 `{출처}/api/auth/external/signin-{google|kakao|naver}` 다.
- 패키지 — Google 은 `Microsoft.AspNetCore.Authentication.Google`, Kakao · Naver 는 aspnet-contrib 의 `AspNet.Security.OAuth.KakaoTalk` · `AspNet.Security.OAuth.Naver`.

| 엔드포인트 (`/api/auth/external`) | 동작 |
|---|---|
| `GET /providers` | 켜진 공급자 `[{name, displayName}]` — SPA 가 버튼을 그린다 |
| `POST /{provider}/login?returnUrl=` | 공급자 로그인으로 보낸다(브라우저 폼 이동). 연결된 계정이면 세션 쿠키를 받고 `returnUrl`(같은 출처 경로만)로, 아니면 `/login?error=EXTERNAL_LOGIN_NOT_LINKED_ERROR&provider=…` 로 돌아온다(이메일을 확인하지 않은 계정은 `EMAIL_NOT_CONFIRMED_ERROR`, 잠긴 계정은 `ACCOUNT_LOCKED_ERROR`) |
| `POST /{provider}/link` | 로그인 필요. 공급자 계정을 연결하고 `/account?linked=…` 로, 다른 사용자에게 이미 연결된 계정이면 `/account?error=EXTERNAL_LOGIN_IN_USE_ERROR` 로 돌아온다 |
| `GET /logins` · `DELETE /{provider}` | 로그인 필요. 연결 목록 · 끊기(204). 마지막 로그인 수단(비밀번호 없음 + 마지막 연결)이면 409 `LAST_LOGIN_METHOD_ERROR` |

- **시작은 POST 다** — cross-site POST 는 Fetch Metadata 가 막으므로 다른 사이트가 로그인 · 연결을 몰래 시작하지 못한다(로그인 CSRF, 원치 않는 연결).
- **state · correlation 은 OAuth 핸들러가 검증한다.** 공급자 콜백(`signin-{공급자}`)이 외부 쿠키에 담고, 앱 콜백(`/api/auth/external/callback`)이 세션으로 바꾸거나 계정에 연결한 뒤 SPA 화면으로 보낸다. 토큰을 URL 에 싣지 않는다.
- 콜백은 공급자에서 돌아오는 cross-site 이동이라 SameSite=Strict 세션 쿠키가 실리지 않는다. 그래서 연결할 사용자 ID 는 시작할 때 보호된 state 에 담아 둔다. 콜백 뒤 SPA 는 same-origin 요청으로 `/api/auth/me` 를 읽으므로 세션 쿠키가 실린다.
- **로컬 · 테스트는 mock 서버로** — docker compose 의 `mock-oauth`(`ghcr.io/navikt/mock-oauth2-server`, http://localhost:8090)가 issuer 하나씩(`/google` · `/kakao` · `/naver`)으로 세 공급자를 흉내 내고, 로그인 화면 없이 고정된 사용자로 돌아온다(`docker/mock-oauth2-server.json` — Kakao 의 `kakao_account`, Naver 의 `response` 모양까지). 통합 테스트도 같은 파일로 컨테이너를 띄워 실제 핸들러로 왕복한다.

**CSRF** — 쿠키가 SameSite=Strict 이고, 그 위에 **Fetch Metadata** 를 한 번 더 본다: 브라우저가 붙이는 `Sec-Fetch-Site` 가 `cross-site` 인 변경 요청(POST · PUT · PATCH · DELETE)은 403(`FetchMetadataMiddleware`, `UseSkeletonWeb` 에 들어 있다). OAuth 엔드포인트(`/connect/*`)는 OAuth 클라이언트가 읽는 형식인 400 `{"error":"invalid_request","error_description":…}` 로 답한다. antiforgery 토큰을 주고받지 않으므로 SPA 코드는 바꿀 것이 없다. 헤더가 없는 요청(서버 간 호출)은 막지 않는다.

```sql
-- 첫 관리자 지정 — 다음 요청부터 반영된다(권한은 쿠키가 아니라 요청마다 판정한다). 이미 떠 있는 앱의 캐시는 최대 Cache:Expiration 뒤에 바뀐다
INSERT INTO user_group_member (UserGroupId, UserId, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
SELECT g.Id, u.Id, 0, SYSUTCDATETIME(), 0, SYSUTCDATETIME()
FROM identity_user AS u CROSS JOIN user_group AS g
WHERE u.NormalizedEmail = UPPER('admin@example.com') AND g.Name = 'Administrators' AND g.DeletedAt IS NULL;
```

## OAuth 서버

Server 는 **OAuth 2.0 인가 서버 + OpenID Connect**(OpenIddict)이기도 하다 — 다른 앱(모바일 · 파트너 서버 · 자사 도구)이 사용자 동의를 받아 이 API 를 부를 토큰을 발급한다. 같은 출처 SPA 는 지금처럼 쿠키를 쓴다.

| 엔드포인트 | 하는 일 |
|---|---|
| `/.well-known/openid-configuration` · JWKS | 탐색 문서 — 실제로 지원하는 것만 싣는다(응답 타입 `code`, grant 셋) |
| `/connect/authorize` | 인가 코드 발급. **PKCE 필수**. 로그인이 없으면 SPA `/login?returnUrl=…` 로, 동의가 필요하면 SPA `/consent?…` 로 보낸다 |
| `/connect/token` | 인가 코드 · 리프레시 토큰(쓸 때마다 회전) · 클라이언트 자격 증명 |
| `/connect/userinfo` | `sub`, scope 에 따라 `email` · `name` |
| `/connect/endsession` | 로그아웃(등록한 `post_logout_redirect_uri` 로만 돌아간다) |
| `GET /api/oauth/consent?client_id=&scope=` | 동의 화면이 보여 줄 클라이언트 이름 · scope 설명(로그인 필요) |

- **로그인은 SPA 화면이 한다.** authorize 는 세션 쿠키로만 로그인을 확인한다. 공급자에서 돌아오는 첫 요청은 cross-site 라 SameSite=Strict 쿠키가 없으므로 SPA 로그인 화면으로 가고, SPA 는 이미 로그인돼 있으면 곧바로 같은 authorize 주소로 다시 이동한다(same-site 이동이라 쿠키가 실린다). 쿠키 정책을 느슨하게(Lax) 바꾸지 않는다.
- **동의** — 클라이언트의 `ConsentType` 이 `Explicit` 이면 SPA `/consent` 가 묻고, 같은 요청을 `consent=accept|deny` 와 함께 `/connect/authorize` 로 POST 한다. 허락은 영구 authorization 으로 저장해 다시 묻지 않는다. `Implicit`(자사 클라이언트)은 묻지 않는다.
- **`prompt`** — 탐색 문서에는 처리하는 값만 싣는다(`prompt_values_supported`: `none` · `login` · `consent`). `none` 은 로그인 · 동의가 필요하면 묻지 않고 `login_required` · `consent_required` 로 돌려준다. `login` 은 세션을 끊고 로그인 화면으로 보낸다 — 돌아올 주소에서는 `login` 을 빼서 다시 끊지 않는다. `consent` 는 이미 허락했거나 `Implicit` 이어도 동의 화면을 보인다. 계정을 여럿 두지 않으므로 `select_account` 는 `invalid_request` 다.
- **scope = 권한** — scope 는 OIDC 표준 넷(`openid` · `profile` · `email` · `offline_access`)과 **권한 코드 카탈로그 전부**다. 클라이언트는 등록된 scope 만 요청할 수 있다.
- **클라이언트는 구성으로 등록한다** — `Authentication:OAuthServer:Clients[]`(`ClientId`, `ClientSecret`(있으면 기밀 클라이언트 — 비밀값), `DisplayName`, `ConsentType`, `GrantTypes`, `RedirectUris`, `PostLogoutRedirectUris`, `Scopes`). 기동할 때 저장소와 맞춘다(만들거나 고친다). 구성에서 빠진 클라이언트는 지우지 않는다. 클라이언트 자격 증명을 쓰는 클라이언트는 비밀이 필수다.
- **키** — `Authentication:OAuthServer:KeySource`(기본값 없음). `Certificates` 는 서명 · 암호화 X.509 인증서 둘(`SigningCertificate` · `EncryptionCertificate`, PKCS#12 base64, 비밀값)로 모든 인스턴스가 같은 키를 쓴다. `Ephemeral` 은 기동할 때마다 새 키라 **로컬 · 테스트 전용**이다(재기동하면 토큰이 무효, 인스턴스끼리 서로의 토큰을 못 읽는다). 액세스 토큰은 암호화된 JWT 다 — 이 서버만 읽는다.
- **issuer 는 `App:PublicOrigin`** — 사용자가 여는 출처와 같다. 로컬은 SPA dev 서버 출처이고, dev 서버가 `/connect` · `/.well-known` 도 백엔드로 넘긴다. 로그인 화면은 돌아갈 곳이 `/connect/…` 이면 라우터가 아니라 브라우저 이동으로 간다.
- **저장소** — `oauth_application` · `oauth_authorization` · `oauth_scope` · `oauth_token`(마이그레이션 `AddOAuthServer`, 프레임워크 테이블).
- **로컬 샘플 클라이언트** — `sample-app`(공개, 인가 코드 + PKCE, 동의 화면, redirect `http://localhost:8091/callback` — 아무것도 듣지 않으므로 주소창에서 `code` 를 읽는다), `sample-service`(기밀, 클라이언트 자격 증명). 예:

```bash
curl -k https://localhost:7274/connect/token -d grant_type=client_credentials \
  -d client_id=sample-service -d client_secret=local-sample-service-secret -d scope=products.manage
```

- 다른 출처의 **브라우저 앱**이 토큰 엔드포인트를 직접 부르는 흐름(CORS)은 아직 없다 — 서버 · 네이티브 클라이언트를 전제로 한다.

**Bearer 토큰으로 API 호출** — 같은 API 를 쿠키와 토큰이 함께 쓴다.

- **인증 스킴은 정책 스킴 하나가 고른다**(`SessionOrBearer`): `Authorization: Bearer …` 가 있으면 OpenIddict 검증(이 서버가 발급한 토큰만), 없으면 세션 쿠키. 토큰이 틀리면 401.
- **권한 판정은 규칙 하나**(`RequestPermissions` — 권한 정책 핸들러와 리소스 핸들러가 쓴다):

| 주체 | 실효 권한 |
|---|---|
| 세션 쿠키 | 사용자의 그룹 · 역할 · 권한 |
| 사용자 토큰(인가 코드) | 사용자의 권한 ∩ 토큰 scope — 관리자라도 받은 scope 밖은 못 쓴다 |
| 클라이언트 토큰(클라이언트 자격 증명) | 토큰 scope 그대로 — 받을 수 있는 scope 는 클라이언트 등록이 정한다 |

- **감사 주체** — 사용자 토큰은 그 사용자(`sub`), 클라이언트 토큰은 시스템(`0`)이고 어느 클라이언트였는지는 로그 scope `ClientId` 에 남는다. 클라이언트 토큰은 사용자가 아니므로 "자기 상품" 이 없고 `/api/auth/me` 는 403 이다.
- **Fetch Metadata** 는 그대로다 — 서버 · 네이티브 클라이언트는 `Sec-Fetch-Site` 를 보내지 않으므로 막히지 않는다.

## 메일

계정 메일(이메일 확인 · 비밀번호 재설정 · 로그인 확인 코드)은 **아웃박스**(`mail_outbox`)에 넣고, 백그라운드(`MailOutboxWorker`)가 **SMTP(MailKit)** 로 보낸다. `Common` 의 Mail 도메인이다 — `MailOutboxBizService`(넣기 · 때가 된 메일 보내기), `IMailOutboxEntityService`, 전송 `IMailClient`(`SmtpMailClient`), 재시도 규칙 `MailRetryPolicy`(Domain), Identity 용 `IEmailSender<ApplicationUser>`(`IdentityEmailSender`). Server 가 `AddSkeletonMail()` 로 붙인다.

- **요청은 행만 넣고 끝난다** — 응답 시간이 SMTP 왕복에 묶이지 않아 가입 여부가 시간으로 드러나지 않고, SMTP 장애가 요청 실패가 되지 않는다.
- **한 통은 한 곳만 보낸다** — 발송 루프는 인스턴스마다 하나씩 돌고, 때가 된 행을 `UPDLOCK · READPAST` 로 잡아 시도 횟수를 올린 뒤 `Mail:Outbox:Lease`(5분) 동안 다른 인스턴스가 다시 잡지 못하게 한다. SMTP 왕복은 트랜잭션 밖이다. 보낸 뒤 기록하기 전에 멈추면 기한이 지나 다시 보낸다 — **최소 한 번 전달**이다.
- **실패하면 다시 보낸다** — 30초부터 두 배씩 기다려 8번까지(약 1시간), 그래도 안 되면 포기하고(`FailedAt`) 오류 로그를 남긴다. 마지막 오류는 `LastError` 에 남는다.
- **보냈거나 포기하면 본문을 비운다** — 토큰이 든 링크를 DB 에 남기지 않는다. 행은 발송 기록으로 남는다(지우지 않는다).
- **구성** `Mail:Outbox` — `PollInterval`(보낼 메일이 없을 때 쉬는 시간, 5초) · `BatchSize`(한 번에 잡는 수, 20 — 가득 차면 쉬지 않고 이어서) · `Lease`(5분).

- **비밀번호 재설정** — `forgot` 이 토큰을 만들어 `{App:PublicOrigin}/reset-password?email=…&code=…` 링크를 메일로 보내고, SPA 의 재설정 화면이 `reset` 을 부른다. 토큰은 **2시간, 한 번만** 쓰인다(재설정이 보안 스탬프를 바꾼다). 재설정하면 다른 기기의 세션은 1분 안에 끊기고(`SecurityStampValidatorOptions.ValidationInterval`), 로그인 잠금도 풀린다.
- **링크 출처는 구성으로만** — `App:PublicOrigin`. 요청의 Host · Origin 헤더로 만들면 공격자가 헤더를 바꿔 남의 재설정 토큰을 자기 서버로 받아 간다.
- **SMTP 는 언제나 인증한다**(`Mail:Smtp:Username` · `Password` 필수) — 인증 여부로 갈리는 분기를 두지 않는다. 로컬 Mailpit 은 아무 계정이나 받도록 띄운다.
- **본문은 로그에 남기지 않는다**(링크에 토큰이 있다). 로그에는 가린 받는 주소(`ab***@example.com`)와 제목만 남는다. 전송이 실패하면 `IMailClient` 가 예외를 던지고 아웃박스가 다시 시도한다 — 보냈다고 가정하지 않는다.
- **로컬** — `docker compose up -d` 의 Mailpit 이 SMTP 1025 로 받고, 받은 메일은 http://localhost:8025 에서 본다. 밖으로 나가지 않는다. 기본 SPA 가 아닌 SPA 로 띄우면 링크 출처를 바꾼다(`App__PublicOrigin=https://localhost:61196`). 템플릿으로 만들면 고른 SPA 의 출처가 들어간다.
- **테스트** — 통합 · 브라우저 테스트가 Mailpit 컨테이너의 REST API(`/api/v1/search` · `/api/v1/message/{ID}`)로 메일을 읽어 링크를 따라간다. 아웃박스를 거치므로 메일이 올 때까지 기다린다(테스트 구성은 `PollInterval` 0.2초). "보내지 않았다" 는 아웃박스에 행이 없는지로 확인한다.

## 아키텍처 선택

**레이어드를 쓴다 — 헥사고날 · 클린 아키텍처가 아니다.** 소규모 팀이 쓰는 CRUD 중심 스켈레톤에서는 포트 인터페이스와 도메인·영속 모델 이중화의 비용이 회수되지 않는다. 그 구조가 주는 이점 중 실제로 필요한 둘은 이미 있다 — 영속성 격리는 `internal` Entity 와 Dto 경계가, 테스트 용이성은 `I~EntityService` 대역이 맡는다. 계층 이름은 Spring 형제 스켈레톤과 같아서 스택이 달라도 같은 어휘로 읽힌다.

**프로젝트는 공유·배포 단위로 나눈다.** 계층마다 프로젝트를 두면 겉모양은 클린 아키텍처인데 의존 역전이 없어서 "포트는 어디 있나" 를 찾게 된다. 공유 라이브러리 + 앱으로 나누면 레이어드가 레이어드로 읽히고, 앱이 늘어도(내부 API · 배치) `Common` 을 같이 쓴다.

**약점과 보완.** 레이어드의 약점은 업무 규칙이 BizService 곳곳에 흩어지는 것이다. 그래서 업무 값 계산과 상태 전이 판단은 **항상** `Domain` 의 순수 클래스에 두고, BizService 는 조회·저장·호출 순서만 맡는다.

**이행 경로.** 저장소나 외부 시스템을 실제로 갈아 끼워야 할 때가 오면, `I~EntityService` 를 쓰는 쪽(BizService 가 있는 곳)으로 옮기는 것만으로 포트가 된다. 그때까지는 옮기지 않는다.

## 코딩 규약

### 규칙을 만드는 원칙

- **예외 케이스를 두지 않는다.** "이 경우엔 건너뛰어도 된다" 가 생기면 그 경우가 기본이 된다. 위임 코드가 한 줄 늘더라도 규칙은 하나로 둔다 — 그래서 MappingService 는 변환이 없어도 항상 둔다.
- **강제와 리뷰를 나눈다.** 기계로 싸게 검사할 수 있는 규칙은 컴파일러 · 분석기 진단 · 규약 테스트로 강제하고, 나머지는 아래 「리뷰 체크포인트」로 둔다.

### 이름

| 접미사 | 계층 · 쓰임 | 네임스페이스 |
|---|---|---|
| `~Controller` | HTTP 입출력 | `.Controllers` |
| `~MappingService` | 화면용 변환 | `.Services.Mapping` |
| `~BizService` | 업무 흐름 · 트랜잭션 경계 | `.Services.Biz` |
| `~EntityService` · `I~EntityService` | 엔티티 하나의 CRUD | `.Persistence.Services` |
| `~Client` | 외부 API (typed HttpClient) | `.Clients` |
| `~Mapper` | Mapperly 매퍼 (`static partial`) | `.Mappers` · `.Persistence.Mappers` |
| `~Model` | HTTP 요청 · 응답 | `.Models` |
| `~Dto` | 계층 사이 전달 | `.Dtos` |
| `~Entity` | DB 매핑 (`internal`) | `.Persistence.Entities` |
| `~Options` | 구성 바인딩 (Options 패턴) | 쓰는 곳 |
| `~Extensions` | DI 등록 · 확장 메서드 | `.Configuration` · `.Web` |

- 네임스페이스는 `{프로젝트}.{도메인}.{계층}` 이다 — 표의 네임스페이스는 도메인 뒤에 붙는다(`Common.Product.Persistence.Services`).
- 맨 `~Service` 는 쓰지 않는다 — 계층을 숨긴다. 업무 규칙(`.Domain`)은 접미사 없이 무엇을 판단하는지로 짓는다(`ProductStatusTransition`).
- 메서드 동사는 한 벌이다 — `Get`(없으면 예외) · `Find`(없으면 null) · `List` · `Create` · `Update` · `Delete`.
- 이름은 Spring 형제 스켈레톤과 같은 어휘를 쓰고, 스택 고유 개념만 .NET 이름으로 바꾼다.

| Spring 형제 | 이 스켈레톤 |
|---|---|
| `~Properties` (`@ConfigurationProperties`) | `~Options` (Options 패턴) |
| `~Configuration` (`@Configuration`) | `~Extensions` (DI 확장 메서드) |
| MapStruct (`unmappedTargetPolicy = ERROR`) | Mapperly 엄격 모드 |
| `~JpaRepository` | 없음 — `AppDbContext` 의 `DbSet` 이 그 자리다 |
| `@Transactional` | `IMainTransaction.ExecuteAsync` |
| `ErrorResponseModel` | ProblemDetails + `code` · `divisionCode` |
| MDC · `TaskDecorator` | 로깅 scope · AsyncLocal (전달 장치가 필요 없다) |
| `StringEnum` · `@JsonValue` · `AbstractStringEnumConverter` | `[JsonStringEnumMemberName]` 하나 — JSON · DB · 쿼리 공통 |
| `SoftDeletable` + `@SQLRestriction` (엔티티마다) | `ISoftDeletable` — 인터셉터 + 전역 쿼리 필터가 자동으로 |
| `@SlowMethod` + `SlowMethodAspect` | `SlowQueryInterceptor` · `SlowRequestMiddleware` — 경계에서 모두 잰다 |
| `@Cacheable` · `@CachePut` · `@CacheEvict` (EntityService) | `EntityCache` — 조회 캐시 + 변경 시 태그 무효화(커밋 뒤) |
| Spring Security(`SecurityFilterChain` · 필터) | ASP.NET Core 인증 · 인가 미들웨어(인증 스킴 · 정책) |
| `UserDetailsService` · `BCryptPasswordEncoder` · `users` · 로그인 시도 카운터 | ASP.NET Core Identity(`identity_*` · `PasswordHasher` · lockout) |
| JWT access/refresh + `user_sessions` + Redis 세션 캐시 | Identity 쿠키 + Redis 의 Data Protection 키(같은 출처 SPA) |

### 선언

- **Model · Dto** — 데이터만 담는 `sealed record`, 생성자 매개변수에 기본값 없음, 계산 속성 없음.
- **Entity** — `internal` `class` + `{ get; set; }`, `BaseEntity`(`long Id`) 상속, navigation property 없음(참조는 `long` 컬럼). 감사가 필요하면 `BaseAuditedEntity`.
- **Domain** — EF · DI · ASP.NET 을 모르는 순수 클래스. `static` 함수나 불변 record.
- **매퍼** — `[Mapper] static partial class`. 계산하지 않는다 — 계산된 값은 MappingService 가 추가 매개변수로 넘긴다.
- **열거형** — 멤버마다 `[JsonStringEnumMemberName("코드")]` 로 외부 코드를 단다. JSON 본문 · 응답, 쿼리 · 라우트 바인딩, DB 컬럼이 모두 이 코드를 쓴다(`Common/Enums/EnumCodes`). C# 멤버 이름을 바꿔도 저장된 데이터와 API 계약이 바뀌지 않는다. EF 변환기는 `AppDbContext` 가 모든 열거형 속성에 자동으로 붙이므로 엔티티 구성에 적지 않는다(길이만 `HasMaxLength`).
- **DB** — 식별자 `BIGINT`, FOREIGN KEY 제약 없음, 열거형은 외부 코드 문자열로 저장, 조회 기본 NoTracking. 업무 테이블에는 물리 삭제 경로를 두지 않는다 — 지울 수 있는 엔티티는 `ISoftDeletable`, 이력은 `BaseCreateAuditedEntity`(지우지 않는다). 논리 삭제된 행과 유일 인덱스가 부딪히면 필터 인덱스(`HasFilter("[DeletedAt] IS NULL")`)로 둔다.
- **테이블 이름** — 업무 엔티티는 클래스 이름에서 `Entity` 를 뺀 snake_case 다(`ProductStatusHistoryEntity` → `product_status_history`). 도메인은 테이블 이름에 넣지 않는다 — 도메인 경계가 바뀌어도 리포트 · 연동이 기대는 운영 DB 이름은 그대로 둔다. 프레임워크가 정의한 엔티티만 그 이름 접두사를 쓴다(`identity_*`, Quartz 는 `quartz` 스키마). SQL Server 예약어(`group`, `order`, `user` …)는 테이블 이름으로 쓰지 않는다 — 클래스 이름을 바꿔 피한다(`UserGroupEntity`).

### Mapperly 엄격 모드와 남은 함정

각 어셈블리의 `Mappers/MapperDefaults.cs` 와 `.editorconfig` 가 정한다 — 대상 누락(RMG012) · 매핑된 멤버 없음(RMG066)은 **빌드 오류**, 속성 단위 null 불일치는 **런타임 예외**, `ToString` 암시 변환은 꺼져 있다. Kotlin 의 MapStruct 에서 겪던 kapt 비용 · `copy()` 오인 · `is` 접두어 Boolean 문제는 C# 에 없다. 남는 함정은 둘이다.

1. **기본값 있는 생성자 매개변수는 소스가 없으면 조용히 기본값이 들어간다** → Model · Dto 생성자에 기본값을 두지 않는다(규약 테스트).
2. **소스 쪽 남는 멤버(RMG020)는 경고다** → 일부러 내보내지 않는 값은 `[MapperIgnoreSource]` 로 이유와 함께 적는다(`ProductModelMapper` 참고).

### 무엇이 규칙을 지키는가

| 규칙 | 검출 |
|---|---|
| Entity 가 EntityService 밖으로 나가지 않는다 · 앱이 DbContext 를 쓰지 않는다 | 컴파일러 — `internal` (CS0050 · CS0051) |
| 매핑 누락 | 컴파일러 — Mapperly RMG012 · RMG066 |
| 계층별 주입 허용 목록 (「요청 흐름」 표) | `LayeringConventionTests` |
| Controller 의 요청 · 응답은 Model 만 | `LayeringConventionTests` |
| 접미사 ↔ 네임스페이스, 맨 `Service` 금지, `I~EntityService` 구현 | `NamingConventionTests` |
| Model · Dto 는 기본값 없는 `sealed record`, 매퍼는 static, 엔티티에 navigation 없음 | `DeclarationConventionTests` |
| Domain 은 Dto · 열거형 · 에러 · System 만 참조 (시그니처 수준) | `DeclarationConventionTests` |
| 열거형 멤버마다 중복 없는 외부 코드 | `DeclarationConventionTests` |
| 컨트롤러는 역할 문자열이 아니라 `[HasPermission]` 으로, 코드는 카탈로그 상수만 | `AuthorizationConventionTests` |
| 계층 네임스페이스는 도메인 아래, 공통 기반 영역에는 abstract 기반 타입만, 다른 도메인의 영속 계층 참조 금지 | `DomainConventionTests` |
| 테이블 이름은 클래스 이름의 snake_case(프레임워크는 접두사), 예약어 금지 | `TableNamingConventionTests` (EF 모델 검사) |

규약 테스트(`UnitTests/Conventions/`)는 앱 어셈블리를 리플렉션으로 훑고, 계층은 네임스페이스로 판정한다. 접미사 ↔ 네임스페이스 검사가 판정의 어긋남도 잡으므로, 검사 대상을 못 찾아 그냥 통과하는 일이 없다. 샘플을 모두 지워도 규약 테스트는 깨지지 않는다(대상 개수 하한을 두지 않는다). 새 앱을 더하면 `ConventionScan` 의 어셈블리 목록에 넣는다.

### 리뷰 체크포인트

기계로 검사하지 않는 규칙이다. 리뷰에서 먼저 본다.

- [ ] BizService · MappingService · 매퍼에 업무 값 계산이나 상태 판단 식이 없다 — Domain 에 있다
- [ ] BizService 의 변경 메서드가 모두 `IMainTransaction.ExecuteAsync` 안에 있다
- [ ] 업무 테이블을 물리 삭제하는 경로(`ExecuteDelete`, 원시 SQL `DELETE`)가 없다
- [ ] 업무 실패는 `CodedException` 계열로 던지고, 메시지는 사용자에게 보여도 되는 문장이다. 예외를 삼키지 않는다
- [ ] 현재 시각은 `TimeProvider` 로만 읽는다
- [ ] 익명으로 연 엔드포인트(`[AllowAnonymous]`)가 정말 익명이어도 되는 것뿐이다
- [ ] API 앱에 주기 실행(`BackgroundService` 타이머 등)이 없다 — 배치 앱의 일이다
- [ ] 새 비밀은 구성 키로만 읽고 README 「설정과 비밀값」 · `appsettings.Gcp.json` 에 반영했다

## 테스트

테스트 프로젝트가 넷이고 **성격이 다르다.** 새 테스트를 어디에 둘지는 「무엇에 의존하는가」로 정한다. 공통 설정은 `test/Directory.Build.props`, 러너는 `global.json` 의 Microsoft.Testing.Platform 이다.

| 프로젝트 | 의존 | 도구 |
|---|---|---|
| `UnitTests` | 없음 | xUnit v3 |
| `FunctionalTests` | 없음 — 앱을 메모리에 띄우고 DB 에는 붙지 않는다 | `WebApplicationFactory` |
| `IntegrationTests` | **Docker** | Testcontainers (SQL Server · Redis · Mailpit · mock OAuth · LocalStack) |
| `E2ETests` | **Docker · Node** — 브라우저 화면 흐름 | Playwright for .NET (Chromium) + 통합 테스트와 같은 컨테이너 |

```bash
dotnet test --solution AspNetCoreSkeletonApplication.slnx                       # 전부 — Docker · Node 필요
dotnet test --project test/unit-tests/AspNetCoreSkeletonApplication.UnitTests  # 하나만
dotnet test --project test/integration-tests/AspNetCoreSkeletonApplication.IntegrationTests -- --filter-class "*HealthCheckTests"  # 클래스만
dotnet test --project test/e2e-tests/AspNetCoreSkeletonApplication.E2ETests -- --filter-class "*AccountTests"     # 브라우저 시나리오만
```

**대역 원칙** — 계층마다 하나로 정해져 있다.

| 대상 | 대역 | 어디서 |
|---|---|---|
| BizService | `I~EntityService` 를 NSubstitute 로, `IMainTransaction` 은 바로 실행하는 가짜로 | `UnitTests` |
| Domain | 대역 없음 — 순수 함수라 표(`[Theory]`)로 모든 경우를 확인 | `UnitTests` |
| `~Client` | 맨 안쪽 `HttpMessageHandler` 만 가짜로 — `AddApiClient` 의 핸들러 체인은 실제 그대로 | `UnitTests` |
| HTTP 계약(상태 코드 · JSON · 에러 응답) | `I~EntityService` 를 메모리 구현으로 바꿔 DB 없이 | `FunctionalTests` |
| EntityService · 트랜잭션 · 감사 필드 | 대역 없음 — SQL Server 컨테이너에 마이그레이션을 적용해 HTTP 로 끝까지 | `IntegrationTests` |

DbContext 는 목킹하지 않는다 — 쿼리·매핑 결함은 실제 DB 에서만 드러난다. 통합 테스트의 SQL Server · Redis · Mailpit · mock OAuth 컨테이너는 `[Collection(InfrastructureCollection.Name)]` 으로 한 번만 띄워 공유하고, 클래스마다 데이터베이스 이름과 캐시 키 접두사를 달리한다 — 앱은 `InfrastructureFixture.ConfigureApp(builder, "<데이터베이스>")` 한 줄로 붙인다.

**브라우저 테스트(`E2ETests`)** 는 운영과 같은 모양으로 띄운다 — SPA 를 `npm run build` 로 정적 빌드하고(의존성이 없으면 먼저 `npm ci`), 그 출력을 웹 루트로 Server 를 Kestrel 의 실제 HTTPS 포트에 띄워 같은 출처로 서빙한다. dev 서버 · 프록시를 거치지 않는다.

- **SPA 마다 같은 시나리오** — 대상 SPA 는 `src/spa` 의 esproj 로 찾고 출력 폴더는 esproj 의 `BuildOutputFolder` 를 읽는다. 템플릿에서 SPA 하나만 고르면 그 하나만 돈다. 세 SPA 는 같은 문구 · 같은 접근성 구조(라벨로 감싼 입력, 역할 있는 버튼 · 안내)라 선택자를 함께 쓴다 — 화면 문구를 바꾸면 테스트도 같이 고친다.
- **한 벌을 함께 쓴다** — 컨테이너 · Chromium · SPA 별 서버(DB `e2e_{SPA}`)는 어셈블리 fixture(`E2EFixture`)가 한 번 띄우고, 테스트는 매번 새 브라우저 컨텍스트(쿠키 없음)를 연다. 서버를 함께 쓰므로 이메일은 테스트마다 새로 만든다(`NewEmail`).
- **인증서** — fixture 가 localhost 자체 서명 인증서를 만들고 브라우저는 인증서 오류를 무시한다. 개발 인증서를 신뢰하지 않은 CI 에서도 `__Host-` 쿠키가 그대로 동작한다.
- **Chromium** 은 fixture 가 처음 실행할 때 받는다(`Microsoft.Playwright.Program.Main(["install", "chromium"])`).
- **화면을 눈으로 보려면** `E2E_HEADED=1` 로 돌린다 — Chromium 창을 띄우고 동작마다 `E2E_SLOWMO` 밀리초(기본 500) 쉰다. 테스트가 병렬로 돌므로 하나만 골라 돌리는 편이 보기 쉽다: `E2E_HEADED=1 dotnet test --project test/e2e-tests/AspNetCoreSkeletonApplication.E2ETests -- --filter-method "*SignUp*"`. 패스키는 Playwright 의 가상 인증기가 답하므로 OS 의 패스키 창(Touch ID 등)은 뜨지 않는다 — 실제 창은 로컬 실행(`dotnet run`)에서 확인한다.
- **실패하면** Playwright 트레이스가 `bin/…/playwright-traces/<테스트>.zip` 에 남는다 — `pwsh bin/Debug/net10.0/playwright.ps1 show-trace <파일>` 또는 `npx playwright show-trace <파일>` 로 화면 · 네트워크를 단계별로 본다.
- 화면 밖에서 준비할 것(가입만 해 두기, 토큰 교환)은 `SpaServer.CreateApiClient()` 로 API 를 직접 부른다. 메일 링크는 `Mailbox.WaitForLinkAsync` 가 Mailpit 에서 꺼낸다.

SPA 테스트는 각 폴더에서 `npm test`(세 SPA 모두 Vitest, Angular 는 `npm test -- --watch=false` 로 단발 실행)다. 타입 검사는 SvelteKit · Nuxt 에서 `npm run check`.

## 설정과 비밀값

### 프로파일 — 비밀값을 어디서 읽을지 고른다

환경(dev·staging·prod)별 프로파일은 없다. 환경 간 차이(호스트, 로그 레벨 등)는 배포할 때 환경변수로 준다. 프로파일(`ASPNETCORE_ENVIRONMENT`)은 **비밀값 출처**만 고른다. 분기는 `Common/Configuration/SecretSourceExtensions.cs` 한 곳에 있고, 앱마다 `Program.cs` 에서 `AddSecretSource<Program>()` 으로 붙인다(user-secrets 는 그 앱 csproj 의 `UserSecretsId`).

| 프로파일 | 비밀값 출처 | 필요한 것 |
|---|---|---|
| (없음 = `Production`) | 환경변수 (컨테이너, k8s Secret) | `ConnectionStrings__Default` · `ConnectionStrings__Redis` · (Server) `Mail__Smtp__Password` |
| `Local` | `appsettings.Local.json` 의 docker compose 값 + user-secrets | `docker compose up -d` |
| `Aws` | AWS Secrets Manager — JSON 비밀 하나의 키 | `Aws:SecretsManager:SecretId`, AWS SDK 기본 자격증명 체인 |
| `Azure` | Azure Key Vault — 볼트의 모든 비밀 | `Azure:KeyVault:Endpoint`, `DefaultAzureCredential` |
| `Gcp` | GCP Secret Manager — `Gcp:SecretManager:Secrets` 에 적은 비밀 | `Gcp:SecretManager:ProjectId`, Application Default Credentials |

저장소 위치 값은 각 `appsettings.<프로파일>.json` 에 적거나 환경변수(`Aws__SecretsManager__SecretId` 처럼 `:` 를 `__` 로)로 준다. **비밀값은 `ConnectionStrings:Default`(SQL Server)와 `ConnectionStrings:Redis` 둘이고, Server 는 SMTP 비밀번호 `Mail:Smtp:Password`, 켠 소셜 공급자의 `Authentication:External:{공급자}:ClientSecret`, OAuth 서버 인증서 `Authentication:OAuthServer:SigningCertificate` · `EncryptionCertificate` 와 기밀 클라이언트의 `Authentication:OAuthServer:Clients:{n}:ClientSecret` 이, Batch 는 웹훅 API 키 `Batch:Webhook:ApiKeys:{n}:Key` 와 대시보드 비밀번호 `Batch:Dashboard:Password` 가 더 있다**(Gcp 를 쓰면 `appsettings.Gcp.json` 의 `Secrets` 에 `Batch--Webhook--ApiKeys--{n}--Key` · `Batch--Dashboard--Password`, 켠 공급자의 `Authentication--External--{공급자}--ClientSecret` 을 추가한다 — 목록의 비밀이 없으면 기동하다 멈추므로 켠 것만 적는다). 빠져 있으면 처음 쓰는 순간 빠진 키와 출처별 이름을 담아 멈춘다 — 조용히 빈 값으로 뜨지 않는다. 위치 값이 비어 있어도 같다.

### 키 이름 대응

| 출처 | 이름 규칙 | 예 |
|---|---|---|
| 구성 키 (코드) | `:` 로 계층 | `ConnectionStrings:Default` |
| 환경변수 | `:` → `__` | `ConnectionStrings__Default` |
| Azure Key Vault · GCP Secret Manager 비밀 이름 | `:` → `--` | `ConnectionStrings--Default` |
| AWS Secrets Manager JSON 비밀의 키 | 구성 키 그대로 | `{"ConnectionStrings:Default": "…"}` |

Server 는 비밀이 아니어도 배포마다 꼭 정해야 하는 값이 있다 — `App:PublicOrigin`(사용자가 여는 SPA 출처, 메일 링크와 OAuth issuer 에 쓴다), `Mail:Smtp:Host` · `Username` · `From`, `Authentication:OAuthServer:KeySource`. 기본값이 없어 빠지면 기동할 때 키 이름을 담아 멈춘다.

비밀이 아닌 환경별 값은 `appsettings.json` 에 기본값을 두고 환경변수로 덮어쓴다 — SMTP 포트 · 보안 `Mail:Smtp:Port`(587) · `Security`(`StartTls`), 외부 API 주소 `Clients:ExampleApi:BaseAddress` ← `Clients__ExampleApi__BaseAddress`, 감사 헤더 신뢰 `Audit:TrustUserHeader` ← `Audit__TrustUserHeader`, 느린 쿼리 · 요청 임계값 `Diagnostics:*`.

평문 자격증명은 어느 appsettings 에도 적지 않는다. `appsettings.Local.json` 의 docker compose 계정만 예외다(로컬 컨테이너 전용 값). 비밀을 추가하면 이 표의 각 출처에 같은 이름으로 넣고, Gcp 를 쓰면 `appsettings.Gcp.json` 의 `Secrets` 에도 추가한다.

### 클라우드별 비밀 등록 예

```bash
CS='Server=tcp:<host>,1433;Database=<db>;User ID=<user>;Password=<password>;Encrypt=True'
REDIS='<host>:6380,password=<password>,ssl=True,abortConnect=False'

# AWS — JSON 비밀 하나에 키를 모은다
aws secretsmanager create-secret --name contoso-shop/prod --secret-string "{\"ConnectionStrings:Default\":\"$CS\",\"ConnectionStrings:Redis\":\"$REDIS\"}"
ASPNETCORE_ENVIRONMENT=Aws Aws__SecretsManager__SecretId=contoso-shop/prod dotnet Contoso.Shop.Server.dll

# Azure — 비밀 이름의 -- 가 : 로 읽힌다
az keyvault secret set --vault-name <vault> --name ConnectionStrings--Default --value "$CS"
az keyvault secret set --vault-name <vault> --name ConnectionStrings--Redis --value "$REDIS"
ASPNETCORE_ENVIRONMENT=Azure Azure__KeyVault__Endpoint=https://<vault>.vault.azure.net/ dotnet Contoso.Shop.Server.dll

# GCP — 비밀 이름은 Azure 와 같고, appsettings.Gcp.json 의 Secrets 에 적은 것만 읽는다
printf '%s' "$CS" | gcloud secrets create ConnectionStrings--Default --data-file=-
printf '%s' "$REDIS" | gcloud secrets create ConnectionStrings--Redis --data-file=-
ASPNETCORE_ENVIRONMENT=Gcp Gcp__SecretManager__ProjectId=<project> dotnet Contoso.Shop.Server.dll
```

### 쓰지 않는 클라우드 지우기

세 클라우드 패키지는 모두 `Common` 프로젝트에 들어 있고, 해당 프로파일일 때만 쓰인다. 하나만 쓴다면 나머지를 지운다.

| 클라우드 | 패키지 (`Directory.Packages.props` · Common csproj) | 코드·설정 |
|---|---|---|
| AWS | `Amazon.Extensions.Configuration.SystemsManager` (+ 테스트의 `AWSSDK.SecretsManager` · `Testcontainers.LocalStack`) | `SecretSourceExtensions` 의 `Aws` 분기, `appsettings.Aws.json`, launchSettings `Aws`, `AwsSecretSourceTests` |
| Azure | `Azure.Extensions.AspNetCore.Configuration.Secrets`, `Azure.Identity` | `Azure` 분기, `appsettings.Azure.json`, launchSettings `Azure` |
| GCP | `Google.Cloud.SecretManager.V1` | `Gcp` 분기, `GcpSecretManagerConfigurationSource.cs`, `appsettings.Gcp.json`, launchSettings `Gcp`, `GcpSecretManagerConfigurationSourceTests` |

`SecretSourceExtensionsTests` 의 해당 `InlineData` 도 함께 지운다.

### OpenAPI

`OpenApi:Enabled` 가 `true` 일 때만 `/openapi/v1.json` 과 `/scalar` 를 연다. `Local` 만 켜져 있다.

## 빌드·실행

```bash
dotnet build AspNetCoreSkeletonApplication.slnx
dotnet run --project src/web-application/AspNetCoreSkeletonApplication.Server --launch-profile https
dotnet publish src/web-application/AspNetCoreSkeletonApplication.Server -c Release -p:SpaClient=angular -o out
ASPNETCORE_ENVIRONMENT=Local dotnet out/AspNetCoreSkeletonApplication.Server.dll
dotnet run --project src/internal-api/AspNetCoreSkeletonApplication.InternalApi --launch-profile https
dotnet run --project src/batch/AspNetCoreSkeletonApplication.Batch --launch-profile https
```

| 항목 | 값 |
|---|---|
| 서버 | https://localhost:7274 · http://localhost:5255 |
| 내부 API | https://localhost:7276 · http://localhost:5257 (SPA 없음, `Local` 에서 `/scalar`) |
| 배치 | https://localhost:7278 · http://localhost:5259 |
| 헬스 체크 | `/health` — `AppDbContext` 연결 확인 |
| 패키지 버전 | `Directory.Packages.props` 중앙 관리 (전이 의존도 고정) |
| macOS 참고 | VS JavaScript SDK 가 SPA 폴더에 `obj\Debug/\package.g.props` 같은 이름의 파일을 만든다. 빌드에 영향 없고 `.gitignore` 에 들어 있다 |

## 관련 문서

- [`CHANGELOG.md`](CHANGELOG.md) — 변경 이력
- [`CLAUDE.md`](CLAUDE.md) — Claude Code 작업 지침
- [`AGENTS.md`](AGENTS.md) — 에이전트 작업 진입점
