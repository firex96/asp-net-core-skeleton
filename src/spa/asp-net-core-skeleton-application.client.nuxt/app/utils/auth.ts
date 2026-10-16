export interface CurrentUser {
  id: number
  email: string
  permissions: string[]
}

export interface ExternalProvider {
  name: string
  displayName: string
}

export interface ExternalLogin {
  provider: string
  displayName: string
}

export interface ConsentDetails {
  clientId: string
  displayName: string
  scopes: { name: string, description: string }[]
}

/** 서버의 ProblemDetails 응답 — 메시지는 detail, 검증 실패는 errors 의 항목들이다. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: string | undefined,
    message: string,
    readonly fieldErrors: string[] = [],
  ) {
    super(message)
  }

  static async from(response: Response): Promise<ApiError> {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    // 검증 실패는 항목마다 문장이 온다 — 같은 문장은 한 번만(이메일 중복은 두 오류가 같은 문장이다)
    const fieldErrors = problem.errors ? [...new Set(Object.values(problem.errors).flat())] : []
    const message = fieldErrors.length > 0 ? '입력한 내용을 확인해 주세요.' : (problem.detail ?? messageOf(response.status))
    return new ApiError(response.status, problem.code, message, fieldErrors)
  }

  /** 서버까지 가지 못했거나 알 수 없는 실패. */
  static unexpected(): ApiError {
    return new ApiError(0, undefined, GENERIC_MESSAGE)
  }
}

const GENERIC_MESSAGE = '요청을 처리하지 못했어요. 잠시 후 다시 시도해 주세요.'

function messageOf(status: number): string {
  switch (status) {
    case 401:
      return '로그인이 필요해요.'
    case 403:
      return '이 작업을 할 권한이 없어요.'
    default:
      return GENERIC_MESSAGE
  }
}

interface ProblemDetails {
  title?: string
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}

/**
 * 쿠키 세션 로그인. 세션 쿠키(HttpOnly)는 브라우저가 들고 다니므로 토큰을 저장하지 않는다 —
 * 로그인 상태는 /api/auth/me 로만 확인한다. 401 이면 로그인 안 함(null)이다.
 */
export async function getCurrentUser(fetchFn: typeof fetch = fetch): Promise<CurrentUser | null> {
  const response = await fetchFn('/api/auth/me')
  if (response.status === 401) {
    return null
  }
  if (!response.ok) {
    throw await ApiError.from(response)
  }
  return response.json()
}

export async function login(email: string, password: string, fetchFn: typeof fetch = fetch): Promise<void> {
  await post(fetchFn, '/api/auth/login', { email, password })
}

/** 가입한 뒤 같은 자격 증명으로 바로 로그인한다. */
export async function register(email: string, password: string, fetchFn: typeof fetch = fetch): Promise<void> {
  await post(fetchFn, '/api/auth/register', { email, password })
  await login(email, password, fetchFn)
}

export async function logout(fetchFn: typeof fetch = fetch): Promise<void> {
  await post(fetchFn, '/api/auth/logout')
}

/** 계정이 있든 없든 성공한다 — 서버가 가입 여부를 드러내지 않는다. */
export async function forgotPassword(email: string, fetchFn: typeof fetch = fetch): Promise<void> {
  await post(fetchFn, '/api/auth/password/forgot', { email })
}

export async function resetPassword(
  email: string,
  code: string,
  newPassword: string,
  fetchFn: typeof fetch = fetch,
): Promise<void> {
  await post(fetchFn, '/api/auth/password/reset', { email, code, newPassword })
}

export function listProviders(fetchFn: typeof fetch = fetch): Promise<ExternalProvider[]> {
  return get(fetchFn, '/api/auth/external/providers')
}

export function listLogins(fetchFn: typeof fetch = fetch): Promise<ExternalLogin[]> {
  return get(fetchFn, '/api/auth/external/logins')
}

export async function unlink(provider: string, fetchFn: typeof fetch = fetch): Promise<void> {
  const response = await fetchFn(`/api/auth/external/${encodeURIComponent(provider)}`, { method: 'DELETE' })
  if (!response.ok) {
    throw await ApiError.from(response)
  }
}

export function getConsent(clientId: string, scope: string, fetchFn: typeof fetch = fetch): Promise<ConsentDetails> {
  return get(
    fetchFn,
    `/api/oauth/consent?client_id=${encodeURIComponent(clientId)}&scope=${encodeURIComponent(scope)}`,
  )
}

/**
 * 서버가 처리하는 주소인가 — OAuth 서버의 authorize 처럼 SPA 화면이 아닌 곳으로 돌아갈 때는 라우터가 아니라
 * 브라우저 이동(전체 페이지)으로 간다. 이 이동은 같은 사이트 안이라 SameSite=Strict 세션 쿠키가 실린다.
 */
export function isServerPath(url: string): boolean {
  return url.startsWith('/connect/') || url.startsWith('/api/')
}

/** 로그인 화면 · 계정 화면이 쿼리로 받는 결과 코드를 사람이 읽을 문장으로. */
export function externalLoginMessage(error: string | null, provider: string | null): string | null {
  const name = providerName(provider)
  switch (error) {
    case null:
      return null
    case 'EXTERNAL_LOGIN_NOT_LINKED_ERROR':
      return `아직 연결하지 않은 ${name} 계정이에요. 이메일로 로그인한 뒤 내 계정에서 연결해 주세요.`
    case 'EXTERNAL_LOGIN_IN_USE_ERROR':
      return `이 ${name} 계정은 이미 다른 회원과 연결돼 있어요.`
    case 'ACCOUNT_LOCKED_ERROR':
      return '로그인에 여러 번 실패해서 계정이 잠겼어요. 30분 뒤에 다시 시도하거나 비밀번호를 재설정해 주세요.'
    default:
      return '소셜 로그인을 마치지 못했어요. 다시 시도해 주세요.'
  }
}

/** 공급자 이름(Google · Kakao · Naver)을 화면에 보일 이름으로. */
export function providerName(provider: string | null): string {
  switch (provider) {
    case 'Kakao':
      return '카카오'
    case 'Naver':
      return '네이버'
    case null:
      return '소셜'
    default:
      return provider
  }
}

/** 로그인 뒤 돌아갈 경로. 같은 출처의 경로만 허용한다 — 외부 주소로 보내는 열린 리다이렉트를 막는다. */
export function safeReturnUrl(value: string | null | undefined): string {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) {
    return '/'
  }
  return value
}

async function get<T>(fetchFn: typeof fetch, url: string): Promise<T> {
  const response = await fetchFn(url)
  if (!response.ok) {
    throw await ApiError.from(response)
  }
  return response.json()
}

async function post(fetchFn: typeof fetch, url: string, body?: object): Promise<void> {
  const response = await fetchFn(url, {
    method: 'POST',
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  })
  if (!response.ok) {
    throw await ApiError.from(response)
  }
}
