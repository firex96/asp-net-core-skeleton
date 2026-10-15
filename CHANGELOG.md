# Changelog

이 프로젝트의 주요 변경을 기록한다. 형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/) 를 따른다.

## [Unreleased]

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
