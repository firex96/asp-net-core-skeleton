# ASP.NET Core + SPA Skeleton

**ASP.NET Core Web API 와 SPA 를 한 솔루션으로 시작할 때 쓰는 템플릿이다.** .NET 10 백엔드, 프론트엔드 선택지(Angular · SvelteKit · Nuxt), 레이어 구조, 테스트 3분류, 클라우드별 비밀값 출처 프로파일(Local · Aws · Azure · Gcp)이 이미 배선돼 있다.

이 문서는 **이 스켈레톤으로 새 프로젝트를 만드는 사람**을 대상으로 한다. 위에서부터 읽고 「시작하기」를 따라 하면 새 프로젝트가 뜬다.

## 형제 스켈레톤

같은 원칙(프로파일은 비밀값 출처만 고른다, 테스트 3분류)을 쓰는 Spring 스켈레톤이 셋 있다. 백엔드 언어와 용도에 맞춰 고른다.

| 스켈레톤 | 쓰는 곳 |
|---|---|
| **`asp-net-core-skeleton`** (이 레포) | ASP.NET Core API + SPA 한 벌, 같은 Common 을 쓰는 내부 API(선택) |
| `spring-boot-public-api-skeleton` | 외부에 노출되는 API (Spring Security + JWT) |
| `spring-boot-internal-api-skeleton` | 게이트웨이 뒤의 내부 API |
| `spring-batch-skeleton` | 배치·스케줄 작업 |

## 기술 스택

| 항목 | 버전 |
|---|---|
| .NET SDK / ASP.NET Core | 10.0.400 / 10.0 (`global.json`) |
| EF Core (SQL Server) | 10.0.13 |
| OpenAPI 문서 · UI | `Microsoft.AspNetCore.OpenApi` · Scalar |
| 테스트 | xUnit v3 + Microsoft.Testing.Platform, Testcontainers |
| Node.js | 24 (각 SPA `package.json` 의 `volta.node`) |
| Angular | 22.2 (Vitest) |
| SvelteKit / Svelte | 3.0 / 5 (adapter-static) |
| Nuxt / Vue | 4.6 / 3.5 (`ssr: false`) |
| 로컬 DB | SQL Server 2022 컨테이너 (`docker-compose.yml`) |

**여기 적힌 숫자는 빌드 파일이 정본이다.** 어긋나면 `global.json` · `Directory.Packages.props` · 각 SPA `package.json` 을 따른다.

## 시작하기

### 1. 사전 준비

```bash
dotnet --version        # 10.0.400 이상
node --version          # 24.15 이상 (Angular 22 CLI 요구) — Volta 를 쓰면 package.json 의 고정 버전이 자동으로 잡힌다
docker info             # 로컬 실행(docker compose)과 통합 테스트(Testcontainers)에 쓴다
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

user-secrets ID 도 새로 만들어진다. 템플릿을 지우려면 `dotnet new uninstall <경로>`.

**복사해서** 쓴다면 `AspNetCoreSkeletonApplication`(PascalCase)과 `asp-net-core-skeleton-application`(kebab-case)을 폴더·파일 이름과 내용에서 모두 바꾸고, 두 앱 csproj(Server · InternalApi)의 `UserSecretsId` 를 같은 새 GUID 로 바꾼다. 쓰지 않는 SPA 는 「프론트엔드 선택」의 표대로 지운다.

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

세 SPA 는 같은 예제(`/weatherforecast` 표)를 같은 방식으로 붙인다 — dev 서버는 `dotnet dev-certs` 인증서로 HTTPS 를 열고, `/weatherforecast` 를 ASP.NET Core 로 프록시한다(`ASPNETCORE_HTTPS_PORT` · `ASPNETCORE_URLS` 로 대상 결정).

### 4. 비밀값 출처를 고른다

애플리케이션은 비밀값을 **.NET 구성 키**(`ConnectionStrings:Default`)로만 읽는다. 어디서 채울지는 실행 프로파일(`ASPNETCORE_ENVIRONMENT`)이 고른다 — 「설정과 비밀값」 참고. 쓰지 않는 클라우드는 「쓰지 않는 클라우드 지우기」대로 지운다.

### 5. 지워도 되는 샘플

| 지울 것 | 함께 지워야 하는 것 |
|---|---|
| Server 의 WeatherForecast 한 벌 — `Controllers/WeatherForecastController`, `Services/Mapping/WeatherForecastMappingService`, `Services/Biz/WeatherForecastBizService`, `Dtos/WeatherForecastDto`, `Models/WeatherForecastModel`, `Mappers/WeatherForecastMapper` | `FunctionalTests/WeatherForecastApiTests.cs`, 각 SPA 의 예제 페이지·`weather-forecast` 모듈과 테스트, SPA dev 서버 프록시의 `/weatherforecast` 경로 |
| Product 한 벌 — Common 의 `Enums/ProductStatus`, `Dtos/Product*Dto`, `Domain/ProductStatusTransition`, `Services/Biz/ProductBizService`, `Persistence/` 의 `Entities/Main/ProductEntity` · `Configurations/ProductEntityConfiguration` · `Mappers/ProductEntityMapper` · `Services/(I)ProductEntityService`, Server 의 `Controllers/ProductController` · `Services/Mapping/ProductMappingService` · `Models/Product*Model` · `Mappers/ProductModelMapper` | `AppDbContext.Products`, `Persistence/Migrations/` 를 지우고 마이그레이션을 새로 만든다. 테스트의 `ProductApiTests`(기능·통합) · `ProductBizServiceTests` · `ProductStatusTransitionTests` · `Support/InMemoryProductEntityService` · `ProductWebApplicationFactory` |
| Server 의 `Clients/ExampleApiClient` · `ExampleApiOptions`, `Dtos/ExampleStatusDto` | Program.cs 의 `AddApiClient<ExampleApiClient, …>` 줄, `appsettings.json` 의 `Clients:ExampleApi`, `UnitTests/Clients/ExampleApiClientTests.cs` |
| `AwsSecretSourceTests` (Aws 를 안 쓰면) | 통합 테스트 csproj 의 `AWSSDK.SecretsManager` · `Testcontainers.LocalStack` |

### 6. 뜨는지 확인한다

```bash
docker compose up -d                                    # SQL Server — mssql-init 이 skeleton DB 를 만든다
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
  spa/                client.angular · client.sveltekit · client.nuxt
test/
  unit-tests/         UnitTests        외부 의존 없음
  functional-tests/   FunctionalTests  WebApplicationFactory 로 HTTP 호출, DB 불필요
  integration-tests/  IntegrationTests Docker 필요 (Testcontainers SQL Server · LocalStack)
```

**프로젝트는 계층이 아니라 공유·배포 단위로 나눈다.** 여러 앱이 함께 쓰는 것은 `Common`, 배포되는 앱은 각자 프로젝트다. 계층은 프로젝트 안의 폴더(네임스페이스)로 나눈다 — 「요청 흐름」 참고. 의존은 앱(`Server` · `InternalApi`) → `Common` 한 방향이고, 앱끼리는 서로 참조하지 않는다. 배치 앱도 같은 자리(`Common` 을 쓰는 앱)에 들어올 예정이다.

| 프로젝트 | 폴더 | 들어가는 것 |
|---|---|---|
| `Common` | `Configuration/` | `AddCommon` · `AddLayeredServices` (DI 등록), `SecretSourceExtensions` (비밀값 출처) |
| | `Persistence/` | `Context/AppDbContext`, `Entities/`, `Services/` (`I~EntityService` · `~EntityService`), `Mappers/` (Entity↔Dto) |
| | `Services/Biz/` · `Domain/` · `Dtos/` · `Enums/` | 여러 앱이 쓰는 BizService · 업무 규칙 · Dto · 열거형 |
| `Server` | `Controllers/` · `Services/Mapping/` · `Models/` · `Mappers/` | HTTP 입출력과 화면용 변환 |
| | `Services/Biz/` · `Dtos/` · `Clients/` | 이 앱만 쓰는 BizService · Dto · 외부 API 클라이언트 |
| `InternalApi` | Server 와 같은 폴더 규칙 | 같은 공유 BizService 를 자기 Model 로 노출한다(예: 감사 주체 ID 까지 내보내는 `ProductModel`) |

**Entity · `AppDbContext` · Entity 매퍼는 `Common` 안에서 `internal` 이다.** 앱은 DbContext 를 볼 수 없고, EntityService 의 public 메서드에 Entity 를 쓰면 접근성 불일치(CS0050 · CS0051)로 컴파일이 깨진다 — 「Entity 는 EntityService 위로 올라가지 않는다」를 컴파일러가 지킨다.

**컴포지션 루트는 각 앱의 `Program.cs`** 다(`InternalApi` 는 top-level 문 대신 네임스페이스 안의 `Program` 클래스 — 테스트가 두 앱을 함께 참조해도 이름이 겹치지 않는다). 비밀값 출처(`AddSecretSource<Program>()`), Common(`AddCommon()`), 앱의 계층 서비스(`AddLayeredServices(typeof(Program).Assembly)`) 순서로 붙인다. 계층 서비스는 이름 접미사로 찾아 scoped 로 등록하므로 서비스를 추가할 때 등록 코드를 쓰지 않는다 — `~MappingService` · `~BizService` 는 자기 타입으로, `~EntityService` 는 `I~EntityService` 로.

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
| Controller (`.Controllers`) | HTTP 처리와 검증. 요청과 응답 모두 Model | MappingService |
| MappingService (`.Services.Mapping`) | 화면용 변환 전부 — 파생 값, 포맷, 여러 BizService 결과 조합. 트랜잭션 없음 | BizService, `IOptions<>`, `TimeProvider` |
| BizService (`.Services.Biz`) | 조회·저장·트랜잭션·호출 순서. 업무 판단은 Domain 을 불러 얻는다. 변경 메서드는 **항상** `IMainTransaction.ExecuteAsync` 안에서 돈다 | `I~EntityService`, `IMainTransaction`, `~Client`, 다른 BizService, `IOptions<>`, `TimeProvider` |
| Domain (`.Domain`) | 업무 값 계산과 상태 전이 판단 전부. EF 를 모르는 순수 클래스, DI 대상이 아니다 | — (Dto · 열거형 · 에러 · System 만 참조) |
| EntityService (`.Persistence.Services`) | Entity 하나의 CRUD. 밖으로는 Dto 만 주고받는다. 모두 `I~EntityService` 를 구현하고, 구현은 `internal` 이다. 조회는 기본 NoTracking, 바꿀 때만 `AsTracking()` | `AppDbContext` |
| 매퍼 (`.Mappers`, `.Persistence.Mappers`) | 필드를 기계적으로 옮긴다. 계산하지 않는다. `static partial` (Mapperly) | — |

모든 계층은 `ILogger<>` 를 더 받을 수 있다. 타입은 셋으로 나눈다 — **Model**(HTTP 입출력, `.Models`), **Dto**(계층 사이 전달, `.Dtos`), **Entity**(DB 매핑, `internal`). Model 과 Dto 는 데이터만 담는 `sealed record` 다.

메서드 이름은 한 벌이다 — `Get`(없으면 `ItemNotFoundException`), `Find`(없으면 null), `List`, `Create`, `Update`, `Delete`(비동기는 `~Async`).

요청 Model 의 검증 특성(`[Required]` · `[Range]` 등)은 record **생성자 매개변수**에 붙인다(`property:` 로 붙이면 MVC 가 요청을 500 으로 막는다). 값 타입 필수 필드는 빠지면 0 이 되므로 `[property: JsonRequired]` 를 함께 붙인다 — `ProductCreateModel` 참고.

예: `GET /weatherforecast` — `WeatherForecastController` 가 `WeatherForecastMappingService.List()` 를 부르고, MappingService 는 `WeatherForecastBizService` 가 만든 `WeatherForecastDto` 에 화씨(표시용 파생 값)를 계산해 `WeatherForecastMapper.ToModel(dto, temperatureF)` 로 `WeatherForecastModel` 을 만든다.

예: `PUT /api/products/{id}/status` — `ProductController` → `ProductMappingService` → 공유 `ProductBizService`(Common) 가 `IMainTransaction` 안에서 `IProductEntityService.GetAsync` 로 현재 상태를 읽고, `ProductStatusTransition.EnsureCanChange`(Domain) 로 판단을 얻은 뒤 `UpdateStatusAsync` 로 바꾼다. 허용되지 않은 전이는 Domain 이 `ConflictException`(409)을 던진다.

### 외부 API 클라이언트

외부 API 는 앱의 `Clients/` 에 typed HttpClient `~Client` 로 두고 BizService 만 주입받는다. 등록은 Program.cs 한 줄이다.

```csharp
builder.Services.AddApiClient<ExampleApiClient, ExampleApiOptions>("Clients:ExampleApi");
```

이 한 줄이 붙이는 것 — `~Options`(`ApiClientOptions` 상속) 바인딩과 기동 시 검증(`BaseAddress` 가 없으면 기동이 멈춘다), 표준 resilience(재시도 · 타임아웃 · 회로 차단), 시도마다 `X-Trace-Id` 전파, 재시도 뒤에도 응답이 없으면 `ApiCallException`(502). 응답은 `ReadRequiredFromJsonAsync<T>()` 로 읽는다 — 오류 상태나 빈 본문도 `ApiCallException` 이 되어 실패가 null 로 새지 않는다.

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
| `ApiCallException` (외부 API 실패) | 502 `EXTERNAL_API_ERROR` (G014) | 없음 — 로그에만 남는다 |
| DataAnnotations 검증 실패 (`[ApiController]` 자동 400) | 400 `NOT_VALID_ERROR` (G011), `errors` 에 필드별 메시지 | — |
| 컨트롤러가 돌려준 상태 코드(`NotFound()` 등) | 상태 코드로 고른 코드 | — |
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

### 감사 — 누가 언제 만들고 고쳤나

`BaseAuditedEntity` 를 상속한 엔티티는 저장 직전에 `CreatedBy` · `CreatedAt` · `UpdatedBy` · `UpdatedAt` 이 채워진다(`AuditSaveChangesInterceptor`, 시각은 `TimeProvider` 의 UTC). 수정 때 생성 필드는 바뀌지 않는다. EntityService 나 매퍼가 직접 넣지 않는다.

감사 주체는 `AuditorContext` 하나만 읽고, HTTP 요청에서는 `AuditorContextMiddleware` 가 다음 순서로 정한다.

| 순위 | 출처 | 조건 |
|---|---|---|
| 1 | 인증 주체 — `NameIdentifier` 또는 `sub` claim | 인증이 붙어 있을 때 |
| 2 | `X-User-Id` 헤더 | `Audit:TrustUserHeader=true` 일 때만 (기본 false) |
| 3 | `AuditorIds.System` (`0`) | 위가 없을 때 — 시스템 자신이거나 주체를 모른다는 뜻 |

값은 `long.TryParse` 로만 읽는다 — 빈 값 · 숫자 아님 · 범위 초과는 버린다(자르면 남의 ID 가 된다). **`X-User-Id` 는 외부 입력이다.** 그대로 믿으면 호출자가 다른 사용자로 감사 기록을 위조할 수 있으므로, 게이트웨이가 인증 결과로 헤더를 덮어쓰고 밖에서 온 같은 이름의 헤더를 지우는 배포에서만 켠다. 내부 API 는 인증이 없어 헤더가 유일한 출처다 — 게이트웨이 뒤에 둘 때 `Audit__TrustUserHeader=true` 로 켠다. 미들웨어는 인증 주체를 읽으므로 `UseAuthorization()` 뒤에 `UseAuditorContext()` 로 붙인다. 배치처럼 HTTP 가 아닌 실행은 진입점에서 `AuditorContext.Begin(userId)` 로 채운다.

## 아키텍처 선택

**레이어드를 쓴다 — 헥사고날 · 클린 아키텍처가 아니다.** 소규모 팀이 쓰는 CRUD 중심 스켈레톤에서는 포트 인터페이스와 도메인·영속 모델 이중화의 비용이 회수되지 않는다. 그 구조가 주는 이점 중 실제로 필요한 둘은 이미 있다 — 영속성 격리는 `internal` Entity 와 Dto 경계가, 테스트 용이성은 `I~EntityService` 대역이 맡는다. 계층 이름은 Spring 형제 스켈레톤과 같아서 스택이 달라도 같은 어휘로 읽힌다.

**프로젝트는 공유·배포 단위로 나눈다.** 계층마다 프로젝트를 두면 겉모양은 클린 아키텍처인데 의존 역전이 없어서 "포트는 어디 있나" 를 찾게 된다. 공유 라이브러리 + 앱으로 나누면 레이어드가 레이어드로 읽히고, 앱이 늘어도(내부 API · 배치) `Common` 을 같이 쓴다.

**약점과 보완.** 레이어드의 약점은 업무 규칙이 BizService 곳곳에 흩어지는 것이다. 그래서 업무 값 계산과 상태 전이 판단은 **항상** `Domain` 의 순수 클래스에 두고, BizService 는 조회·저장·호출 순서만 맡는다.

**이행 경로.** 저장소나 외부 시스템을 실제로 갈아 끼워야 할 때가 오면, `I~EntityService` 를 쓰는 쪽(BizService 가 있는 곳)으로 옮기는 것만으로 포트가 된다. 그때까지는 옮기지 않는다.

## 테스트

테스트 프로젝트가 셋이고 **성격이 다르다.** 새 테스트를 어디에 둘지는 「무엇에 의존하는가」로 정한다. 공통 설정은 `test/Directory.Build.props`, 러너는 `global.json` 의 Microsoft.Testing.Platform 이다.

| 프로젝트 | 의존 | 도구 |
|---|---|---|
| `UnitTests` | 없음 | xUnit v3 |
| `FunctionalTests` | 없음 — 앱을 메모리에 띄우고 DB 에는 붙지 않는다 | `WebApplicationFactory` |
| `IntegrationTests` | **Docker** | Testcontainers (SQL Server · LocalStack) |

```bash
dotnet test --solution AspNetCoreSkeletonApplication.slnx                       # 전부 — Docker 필요
dotnet test --project test/unit-tests/AspNetCoreSkeletonApplication.UnitTests  # 하나만
dotnet test --project test/integration-tests/AspNetCoreSkeletonApplication.IntegrationTests -- --filter-class "*HealthCheckTests"  # 클래스만
```

**대역 원칙** — 계층마다 하나로 정해져 있다.

| 대상 | 대역 | 어디서 |
|---|---|---|
| BizService | `I~EntityService` 를 NSubstitute 로, `IMainTransaction` 은 바로 실행하는 가짜로 | `UnitTests` |
| Domain | 대역 없음 — 순수 함수라 표(`[Theory]`)로 모든 경우를 확인 | `UnitTests` |
| `~Client` | 맨 안쪽 `HttpMessageHandler` 만 가짜로 — `AddApiClient` 의 핸들러 체인은 실제 그대로 | `UnitTests` |
| HTTP 계약(상태 코드 · JSON · 에러 응답) | `I~EntityService` 를 메모리 구현으로 바꿔 DB 없이 | `FunctionalTests` |
| EntityService · 트랜잭션 · 감사 필드 | 대역 없음 — SQL Server 컨테이너에 마이그레이션을 적용해 HTTP 로 끝까지 | `IntegrationTests` |

DbContext 는 목킹하지 않는다 — 쿼리·매핑 결함은 실제 DB 에서만 드러난다. 통합 테스트의 SQL Server 컨테이너는 `[Collection(SqlServerCollection.Name)]` 으로 한 번만 띄워 공유하고, 클래스마다 데이터베이스 이름을 달리한다(`SqlServerFixture.ConnectionStringFor`).

SPA 테스트는 각 폴더에서 `npm test`(세 SPA 모두 Vitest, Angular 는 `npm test -- --watch=false` 로 단발 실행)다. 타입 검사는 SvelteKit · Nuxt 에서 `npm run check`.

## 설정과 비밀값

### 프로파일 — 비밀값을 어디서 읽을지 고른다

환경(dev·staging·prod)별 프로파일은 없다. 환경 간 차이(호스트, 로그 레벨 등)는 배포할 때 환경변수로 준다. 프로파일(`ASPNETCORE_ENVIRONMENT`)은 **비밀값 출처**만 고른다. 분기는 `Common/Configuration/SecretSourceExtensions.cs` 한 곳에 있고, 앱마다 `Program.cs` 에서 `AddSecretSource<Program>()` 으로 붙인다(user-secrets 는 그 앱 csproj 의 `UserSecretsId`).

| 프로파일 | 비밀값 출처 | 필요한 것 |
|---|---|---|
| (없음 = `Production`) | 환경변수 (컨테이너, k8s Secret) | `ConnectionStrings__Default` |
| `Local` | `appsettings.Local.json` 의 docker compose 값 + user-secrets | `docker compose up -d` |
| `Aws` | AWS Secrets Manager — JSON 비밀 하나의 키 | `Aws:SecretsManager:SecretId`, AWS SDK 기본 자격증명 체인 |
| `Azure` | Azure Key Vault — 볼트의 모든 비밀 | `Azure:KeyVault:Endpoint`, `DefaultAzureCredential` |
| `Gcp` | GCP Secret Manager — `Gcp:SecretManager:Secrets` 에 적은 비밀 | `Gcp:SecretManager:ProjectId`, Application Default Credentials |

저장소 위치 값은 각 `appsettings.<프로파일>.json` 에 적거나 환경변수(`Aws__SecretsManager__SecretId` 처럼 `:` 를 `__` 로)로 준다. **`ConnectionStrings:Default` 가 없으면 기동이 멈추고** 빠진 키와 출처별 이름을 알려 준다 — 조용히 빈 값으로 뜨지 않는다. 위치 값이 비어 있어도 같다.

### 키 이름 대응

| 출처 | 이름 규칙 | 예 |
|---|---|---|
| 구성 키 (코드) | `:` 로 계층 | `ConnectionStrings:Default` |
| 환경변수 | `:` → `__` | `ConnectionStrings__Default` |
| Azure Key Vault · GCP Secret Manager 비밀 이름 | `:` → `--` | `ConnectionStrings--Default` |
| AWS Secrets Manager JSON 비밀의 키 | 구성 키 그대로 | `{"ConnectionStrings:Default": "…"}` |

비밀이 아닌 환경별 값은 `appsettings.json` 에 기본값을 두고 환경변수로 덮어쓴다 — 외부 API 주소 `Clients:ExampleApi:BaseAddress` ← `Clients__ExampleApi__BaseAddress`, 감사 헤더 신뢰 `Audit:TrustUserHeader` ← `Audit__TrustUserHeader`.

평문 자격증명은 어느 appsettings 에도 적지 않는다. `appsettings.Local.json` 의 docker compose 계정만 예외다(로컬 컨테이너 전용 값). 비밀을 추가하면 이 표의 각 출처에 같은 이름으로 넣고, Gcp 를 쓰면 `appsettings.Gcp.json` 의 `Secrets` 에도 추가한다.

### 클라우드별 비밀 등록 예

```bash
CS='Server=tcp:<host>,1433;Database=<db>;User ID=<user>;Password=<password>;Encrypt=True'

# AWS — JSON 비밀 하나에 키를 모은다
aws secretsmanager create-secret --name contoso-shop/prod --secret-string "{\"ConnectionStrings:Default\":\"$CS\"}"
ASPNETCORE_ENVIRONMENT=Aws Aws__SecretsManager__SecretId=contoso-shop/prod dotnet Contoso.Shop.Server.dll

# Azure — 비밀 이름의 -- 가 : 로 읽힌다
az keyvault secret set --vault-name <vault> --name ConnectionStrings--Default --value "$CS"
ASPNETCORE_ENVIRONMENT=Azure Azure__KeyVault__Endpoint=https://<vault>.vault.azure.net/ dotnet Contoso.Shop.Server.dll

# GCP — 비밀 이름은 Azure 와 같고, appsettings.Gcp.json 의 Secrets 에 적은 것만 읽는다
printf '%s' "$CS" | gcloud secrets create ConnectionStrings--Default --data-file=-
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
```

| 항목 | 값 |
|---|---|
| 서버 | https://localhost:7274 · http://localhost:5255 |
| 내부 API | https://localhost:7276 · http://localhost:5257 (SPA 없음, `Local` 에서 `/scalar`) |
| 헬스 체크 | `/health` — `AppDbContext` 연결 확인 |
| 패키지 버전 | `Directory.Packages.props` 중앙 관리 (전이 의존도 고정) |
| macOS 참고 | VS JavaScript SDK 가 SPA 폴더에 `obj\Debug/\package.g.props` 같은 이름의 파일을 만든다. 빌드에 영향 없고 `.gitignore` 에 들어 있다 |

## 관련 문서

- [`CHANGELOG.md`](CHANGELOG.md) — 변경 이력
- [`CLAUDE.md`](CLAUDE.md) — Claude Code 작업 지침
- [`AGENTS.md`](AGENTS.md) — 에이전트 작업 진입점
