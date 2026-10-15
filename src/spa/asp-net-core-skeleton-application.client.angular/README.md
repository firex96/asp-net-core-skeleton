# asp-net-core-skeleton-application.client.angular

ASP.NET Core 서버(`AspNetCoreSkeletonApplication.Server`)가 띄우고 서빙하는 SPA 클라이언트다. 서버 csproj 의 `SpaClient` 가 `angular` 일 때 쓰인다 — 루트 README 「프론트엔드를 고른다」 참고.

```bash
npm ci
npm start       # dev 서버 https://localhost:61194 (SpaProxy 가 서버 실행 때 이 명령으로 띄운다)
npm run build   # 정적 빌드 → dist/asp-net-core-skeleton-application.client.angular/browser (게시할 때 서버 wwwroot 로 들어간다)
npm test -- --watch=false   # Vitest 단발 실행
```

dev 서버는 `dotnet dev-certs https` 인증서로 HTTPS 를 열고 `/weatherforecast` 를 ASP.NET Core 로 프록시한다. 혼자 띄울 때는 서버도 따로 실행해 둔다.
