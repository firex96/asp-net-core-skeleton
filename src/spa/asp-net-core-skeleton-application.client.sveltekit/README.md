# asp-net-core-skeleton-application.client.sveltekit

ASP.NET Core 서버(`AspNetCoreSkeletonApplication.Server`)가 띄우고 서빙하는 SPA 클라이언트다. 서버 csproj 의 `SpaClient` 가 `sveltekit` 일 때 쓰인다 — 루트 README 「프론트엔드를 고른다」 참고.

```bash
npm ci
npm start       # dev 서버 https://localhost:61195 (SpaProxy 가 서버 실행 때 이 명령으로 띄운다)
npm run build   # 정적 빌드 → build (게시할 때 서버 wwwroot 로 들어간다)
npm test        # Vitest 단발 실행
npm run check   # svelte-check 타입 검사
```

dev 서버는 `dotnet dev-certs https` 인증서로 HTTPS 를 열고 `/weatherforecast` 를 ASP.NET Core 로 프록시한다. 혼자 띄울 때는 서버도 따로 실행해 둔다.
