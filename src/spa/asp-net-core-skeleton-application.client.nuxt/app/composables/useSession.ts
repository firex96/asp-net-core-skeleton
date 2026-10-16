/** 화면이 함께 보는 로그인 상태. undefined = 아직 확인 전, null = 로그인 안 함. */
export function useSession() {
  const user = useState<CurrentUser | null | undefined>('session-user', () => undefined)

  async function refresh(): Promise<CurrentUser | null> {
    user.value = await getCurrentUser()
    return user.value
  }

  async function signIn(email: string, password: string): Promise<CurrentUser | null> {
    await login(email, password)
    return refresh()
  }

  /** 가입만 한다 — 이메일을 확인한 뒤에 로그인한다. */
  async function signUp(email: string, password: string): Promise<void> {
    await register(email, password)
  }

  async function signOut(): Promise<void> {
    await logout()
    user.value = null
  }

  return { user, refresh, signIn, signUp, signOut }
}
