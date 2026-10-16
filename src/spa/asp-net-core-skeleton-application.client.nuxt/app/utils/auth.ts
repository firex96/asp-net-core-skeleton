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
    const fieldErrors = problem.errors ? Object.values(problem.errors).flat() : []
    const message = problem.detail ?? problem.title ?? `The request failed (${response.status}).`
    return new ApiError(response.status, problem.code, message, fieldErrors)
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
  const name = provider ?? 'social'
  switch (error) {
    case null:
      return null
    case 'EXTERNAL_LOGIN_NOT_LINKED_ERROR':
      return `Your ${name} account is not linked yet. Log in with your password, then link it on the Account page.`
    case 'EXTERNAL_LOGIN_IN_USE_ERROR':
      return `That ${name} account is already linked to another user.`
    case 'ACCOUNT_LOCKED_ERROR':
      return 'Your account is locked after too many failed attempts. Try again later.'
    default:
      return 'Social login failed. Please try again.'
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
