// ASP.NET Core 개발 인증서로 HTTPS dev 서버를 띄우고 API 요청을 백엔드로 프록시한다.
import child_process from 'node:child_process'
import fs from 'node:fs'
import path from 'node:path'
import { env } from 'node:process'

const certificateName = 'asp-net-core-skeleton-application.client.nuxt'
const port = 61196
const apiPaths = ['/weatherforecast']

export function aspNetCoreDevServer() {
  const baseFolder
    = env.APPDATA !== undefined && env.APPDATA !== ''
      ? `${env.APPDATA}/ASP.NET/https`
      : `${env.HOME}/.aspnet/https`

  const certFilePath = path.join(baseFolder, `${certificateName}.pem`)
  const keyFilePath = path.join(baseFolder, `${certificateName}.key`)

  if (!fs.existsSync(baseFolder)) {
    fs.mkdirSync(baseFolder, { recursive: true })
  }

  if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    const result = child_process.spawnSync(
      'dotnet',
      ['dev-certs', 'https', '--export-path', certFilePath, '--format', 'Pem', '--no-password'],
      { stdio: 'inherit' },
    )
    if (result.status !== 0) {
      throw new Error('Could not create certificate.')
    }
  }

  const target = env.ASPNETCORE_HTTPS_PORT
    ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
    : env.ASPNETCORE_URLS
      ? env.ASPNETCORE_URLS.split(';')[0]
      : 'https://localhost:7274'

  return {
    devServer: {
      host: '127.0.0.1',
      port,
      https: { cert: certFilePath, key: keyFilePath },
    },
    devProxy: Object.fromEntries(
      apiPaths.map(apiPath => [apiPath, { target: `${target}${apiPath}`, changeOrigin: true, secure: false }]),
    ),
  }
}
