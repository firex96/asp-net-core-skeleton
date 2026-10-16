import {
	completeTwoFactor,
	getCurrentUser,
	login,
	logout,
	register,
	type CurrentUser,
	type TwoFactorProvider
} from './auth.js';

/** 화면이 함께 보는 로그인 상태. undefined = 아직 확인 전, null = 로그인 안 함. */
export const session = $state<{ user: CurrentUser | null | undefined }>({ user: undefined });

export async function refreshSession(): Promise<CurrentUser | null> {
	session.user = await getCurrentUser();
	return session.user;
}

/** 로그인한다. 2단계 인증이 필요하면 그 수단들을 돌려주고 로그인 상태는 그대로 둔다(두 번째 단계는 finishTwoFactor). */
export async function signIn(email: string, password: string): Promise<TwoFactorProvider[] | null> {
	const providers = await login(email, password);
	if (providers) {
		return providers;
	}

	await refreshSession();
	return null;
}

export async function finishTwoFactor(provider: TwoFactorProvider, code: string, rememberBrowser: boolean): Promise<void> {
	await completeTwoFactor(provider, code, rememberBrowser);
	await refreshSession();
}

/** 가입만 한다 — 이메일을 확인한 뒤에 로그인한다. */
export async function signUp(email: string, password: string): Promise<void> {
	await register(email, password);
}

export async function signOut(): Promise<void> {
	await logout();
	session.user = null;
}
