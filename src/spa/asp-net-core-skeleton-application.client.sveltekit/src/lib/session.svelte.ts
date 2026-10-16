import { getCurrentUser, login, logout, register, type CurrentUser } from './auth.js';

/** 화면이 함께 보는 로그인 상태. undefined = 아직 확인 전, null = 로그인 안 함. */
export const session = $state<{ user: CurrentUser | null | undefined }>({ user: undefined });

export async function refreshSession(): Promise<CurrentUser | null> {
	session.user = await getCurrentUser();
	return session.user;
}

export async function signIn(email: string, password: string): Promise<CurrentUser | null> {
	await login(email, password);
	return refreshSession();
}

export async function signUp(email: string, password: string): Promise<CurrentUser | null> {
	await register(email, password);
	return refreshSession();
}

export async function signOut(): Promise<void> {
	await logout();
	session.user = null;
}
