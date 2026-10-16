// https://nuxt.com/docs/api/configuration/nuxt-config
import { aspNetCoreDevServer } from './aspnetcore-dev-server'

// dev 서버(SpaProxy 가 npm start 로 띄움)에서만 HTTPS 인증서·API 프록시를 붙인다.
const dev = process.env.NODE_ENV === 'development' && !process.env.VITEST ? aspNetCoreDevServer() : undefined

export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
  // SPA 모드: ASP.NET Core 가 nuxt generate 산출물(.output/public)을 wwwroot 로 서빙한다.
  ssr: false,
  // 화면 공통 스타일 — 세 SPA 가 같은 파일을 쓴다
  css: ['~/assets/css/app.css'],
  app: {
    head: { title: 'AspNetCoreSkeletonApplication', htmlAttrs: { lang: 'ko' } },
  },
  devServer: dev?.devServer,
  nitro: {
    devProxy: dev?.devProxy,
  },
})
