/** 화면이 함께 보는 로그인 상태. undefined = 아직 확인 전, null = 로그인 안 함. */
export function useSession() {
  const user = useState<CurrentUser | null | undefined>('session-user', () => undefined)

  async function refresh(): Promise<CurrentUser | null> {
    user.value = await getCurrentUser()
    return user.value
  }

  /** 로그인한다. 2단계 인증이 필요하면 그 수단들을 돌려주고 로그인 상태는 그대로 둔다(두 번째 단계는 finishTwoFactor). */
  async function signIn(email: string, password: string): Promise<TwoFactorProvider[] | null> {
    const providers = await login(email, password)
    if (providers) {
      return providers
    }

    await refresh()
    return null
  }

  /** 패스키로 로그인한다 — 브라우저가 이 사이트의 패스키를 고르게 한다. 패스키는 2단계 인증을 건너뛴다(그 자체로 다중 요소다). */
  async function signInWithPasskey(): Promise<void> {
    const credential = await getPasskey(await passkeyLoginOptions())
    await passkeyLogin(credential)
    await refresh()
  }

  async function finishTwoFactor(provider: TwoFactorProvider, code: string, rememberBrowser: boolean): Promise<void> {
    await completeTwoFactor(provider, code, rememberBrowser)
    await refresh()
  }

  /** 가입만 한다 — 이메일을 확인한 뒤에 로그인한다. */
  async function signUp(email: string, password: string): Promise<void> {
    await register(email, password)
  }

  async function signOut(): Promise<void> {
    await logout()
    user.value = null
  }

  return { user, refresh, signIn, signInWithPasskey, finishTwoFactor, signUp, signOut }
}
