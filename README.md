# ASP.NET Core + SPA Skeleton

**ASP.NET Core Web API 와 SPA 를 한 솔루션으로 시작할 때 쓰는 템플릿이다.** .NET 10 백엔드, 프론트엔드 선택지(Angular · SvelteKit · Nuxt), 레이어 구조, 테스트 3분류, 클라우드별 비밀값 출처 프로파일(Local · Aws · Azure · Gcp)이 이미 배선돼 있다.

이 문서는 **이 스켈레톤으로 새 프로젝트를 만드는 사람**을 대상으로 한다. 위에서부터 읽고 「시작하기」를 따라 하면 새 프로젝트가 뜬다.

## 형제 스켈레톤

같은 원칙(프로파일은 비밀값 출처만 고른다, 테스트 3분류)을 쓰는 Spring 스켈레톤이 셋 있다. 백엔드 언어와 용도에 맞춰 고른다.

| 스켈레톤 | 쓰는 곳 |
|---|---|
| **`asp-net-core-skeleton`** (이 레포) | ASP.NET Core API + SPA 한 벌 |
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

user-secrets ID 도 새로 만들어진다. 템플릿을 지우려면 `dotnet new uninstall <경로>`.

**복사해서** 쓴다면 `AspNetCoreSkeletonApplication`(PascalCase)과 `asp-net-core-skeleton-application`(kebab-case)을 폴더·파일 이름과 내용에서 모두 바꾸고, 서버 csproj 의 `UserSecretsId` 를 새 GUID 로 바꾼다. 쓰지 않는 SPA 는 「프론트엔드 선택」의 표대로 지운다.

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
| `WeatherForecastController`, `WeatherForecast` | `FunctionalTests/WeatherForecastApiTests.cs`, 각 SPA 의 예제 페이지·`weather-forecast` 모듈과 테스트, SPA dev 서버 프록시의 `/weatherforecast` 경로 |
| `AwsSecretSourceTests` (Aws 를 안 쓰면) | 통합 테스트 csproj 의 `AWSSDK.SecretsManager` · `Testcontainers.LocalStack` |

### 6. 뜨는지 확인한다

```bash
docker compose up -d                                    # SQL Server — mssql-init 이 skeleton DB 를 만든다
dotnet build AspNetCoreSkeletonApplication.slnx
cd src/web-application/AspNetCoreSkeletonApplication.Server
dotnet run --launch-profile https                       # Local 프로파일 + SpaProxy
curl -k https://localhost:7274/health                   # Healthy
```

브라우저에는 Scalar(`/scalar`)가 열리고, SPA 는 dev 서버 주소(Angular 기본 https://localhost:61194)에서 뜬다.

## 구조

```
src/
  application-core/   ApplicationCore              도메인·유스케이스 (아직 비어 있다)
  infrastructure/     Infrastructure.EntityFramework  AppDbContext, EF Core 매핑
                      Infrastructure.ExternalService  외부 API 클라이언트 (아직 비어 있다)
  web-application/    Server                       ASP.NET Core 호스트: 컨트롤러, 구성, DI, SPA 서빙
  spa/                client.angular · client.sveltekit · client.nuxt
test/
  unit-tests/         UnitTests        외부 의존 없음
  functional-tests/   FunctionalTests  WebApplicationFactory 로 HTTP 호출, DB 불필요
  integration-tests/  IntegrationTests Docker 필요 (Testcontainers SQL Server · LocalStack)
```

의존 방향은 `Server → Infrastructure → ApplicationCore` 다. 연결 문자열·비밀값 출처·DI 구성은 컴포지션 루트인 `Server` 에 둔다.

엔티티를 추가하면 `AppDbContext` 에 `DbSet` 을 두고 마이그레이션을 만든다.

```bash
dotnet tool update -g dotnet-ef
dotnet ef migrations add Initial \
  --project src/infrastructure/AspNetCoreSkeletonApplication.Infrastructure.EntityFramework \
  --startup-project src/web-application/AspNetCoreSkeletonApplication.Server
```

`dotnet ef` 는 시작 프로젝트를 띄워 구성을 읽으므로 `ASPNETCORE_ENVIRONMENT=Local` 처럼 연결 문자열이 있는 프로파일로 실행한다.

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

SPA 테스트는 각 폴더에서 `npm test`(세 SPA 모두 Vitest, Angular 는 `npm test -- --watch=false` 로 단발 실행)다. 타입 검사는 SvelteKit · Nuxt 에서 `npm run check`.

## 설정과 비밀값

### 프로파일 — 비밀값을 어디서 읽을지 고른다

환경(dev·staging·prod)별 프로파일은 없다. 환경 간 차이(호스트, 로그 레벨 등)는 배포할 때 환경변수로 준다. 프로파일(`ASPNETCORE_ENVIRONMENT`)은 **비밀값 출처**만 고른다. 분기는 `Server/Configuration/SecretSourceExtensions.cs` 한 곳에 있다.

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

세 클라우드 패키지는 모두 서버 프로젝트에 들어 있고, 해당 프로파일일 때만 쓰인다. 하나만 쓴다면 나머지를 지운다.

| 클라우드 | 패키지 (`Directory.Packages.props` · 서버 csproj) | 코드·설정 |
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
```

| 항목 | 값 |
|---|---|
| 서버 | https://localhost:7274 · http://localhost:5255 |
| 헬스 체크 | `/health` — `AppDbContext` 연결 확인 |
| 패키지 버전 | `Directory.Packages.props` 중앙 관리 (전이 의존도 고정) |
| macOS 참고 | VS JavaScript SDK 가 SPA 폴더에 `obj\Debug/\package.g.props` 같은 이름의 파일을 만든다. 빌드에 영향 없고 `.gitignore` 에 들어 있다 |

참고: [Next-level Clean Architecture boilerplate](https://devblogs.microsoft.com/ise/next-level-clean-architecture-boilerplate/)

## 관련 문서

- [`CHANGELOG.md`](CHANGELOG.md) — 변경 이력
- [`CLAUDE.md`](CLAUDE.md) — Claude Code 작업 지침
- [`AGENTS.md`](AGENTS.md) — 에이전트 작업 진입점
