export interface CurrentUser {
  id: number
  email: string
  permissions: string[]
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

/** 로그인 뒤 돌아갈 경로. 같은 출처의 경로만 허용한다 — 외부 주소로 보내는 열린 리다이렉트를 막는다. */
export function safeReturnUrl(value: string | null | undefined): string {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) {
    return '/'
  }
  return value
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
