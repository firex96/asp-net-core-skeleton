import { describe, expect, it, vi } from 'vitest';
import {
	ApiError,
	completeTwoFactor,
	confirmEmail,
	externalLoginMessage,
	forgotPassword,
	getConsent,
	getCurrentUser,
	getTwoFactorChallenge,
	isServerPath,
	login,
	logout,
	register,
	resendConfirmation,
	resetPassword,
	safeReturnUrl,
	setupAuthenticator,
	unlink
} from './auth';

const user = { id: 7, email: 'user@example.com', permissions: [] };

describe('getCurrentUser', () => {
	it('returns the signed-in user', async () => {
		const fetchFn = vi.fn(async () => Response.json(user));

		expect(await getCurrentUser(fetchFn)).toEqual(user);
		expect(fetchFn).toHaveBeenCalledWith('/api/auth/me');
	});

	it('treats 401 as signed out', async () => {
		const fetchFn = vi.fn(async () => new Response(null, { status: 401 }));

		expect(await getCurrentUser(fetchFn)).toBeNull();
	});
});

describe('login', () => {
	it('posts the credentials as JSON', async () => {
		const fetchFn = vi.fn(async () => new Response(null, { status: 204 }));

		await login(user.email, 'Passw0rd!', fetchFn);

		expect(fetchFn).toHaveBeenCalledWith('/api/auth/login', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ email: user.email, password: 'Passw0rd!' })
		});
	});

	it('throws ApiError with the server message and code', async () => {
		const fetchFn = vi.fn(async () =>
			Response.json({ status: 423, code: 'ACCOUNT_LOCKED_ERROR', detail: 'locked' }, { status: 423 })
		);

		const error = await login(user.email, 'wrong', fetchFn).catch((caught: unknown) => caught);

		expect(error).toBeInstanceOf(ApiError);
		expect(error).toMatchObject({ status: 423, code: 'ACCOUNT_LOCKED_ERROR', message: 'locked' });
	});
});

describe('two-factor sign-in', () => {
	it('returns the second-step providers when the server answers 202', async () => {
		const fetchFn = vi.fn(async () => Response.json({ providers: ['authenticator', 'recovery'] }, { status: 202 }));

		expect(await login(user.email, 'Passw0rd!', fetchFn)).toEqual(['authenticator', 'recovery']);
	});

	it('treats a missing pending sign-in as null and posts the second step', async () => {
		const fetchFn = vi.fn<typeof fetch>(async () => new Response(null, { status: 401 }));
		expect(await getTwoFactorChallenge(fetchFn)).toBeNull();

		fetchFn.mockResolvedValue(new Response(null, { status: 204 }));
		await completeTwoFactor('authenticator', '123456', true, fetchFn);

		expect(fetchFn.mock.calls[1]?.[0]).toBe('/api/auth/login/2fa');
		expect(fetchFn.mock.calls[1]?.[1]?.body).toBe(
			JSON.stringify({ provider: 'authenticator', code: '123456', rememberBrowser: true })
		);
	});

	it('reads the authenticator setup', async () => {
		const setup = {
			sharedKey: 'abcd efgh',
			authenticatorUri: 'otpauth://totp/x',
			qrCodeDataUri: 'data:image/svg+xml;base64,'
		};
		const fetchFn = vi.fn(async () => Response.json(setup));

		expect(await setupAuthenticator(fetchFn)).toEqual(setup);
	});
});

describe('register', () => {
	it('only creates the account — signing in waits for the email confirmation', async () => {
		const fetchFn = vi.fn<typeof fetch>(async () => new Response(null, { status: 201 }));

		await register(user.email, 'Passw0rd!', fetchFn);

		expect(fetchFn.mock.calls.map(([url]) => url)).toEqual(['/api/auth/register']);
	});

	it('collects validation messages from errors', async () => {
		const fetchFn = vi.fn(async () =>
			Response.json(
				{ title: 'One or more validation errors occurred.', errors: { PasswordTooShort: ['too short'], PasswordRequiresDigit: ['digit'] } },
				{ status: 400 }
			)
		);

		const error = (await register(user.email, 'short', fetchFn).catch((caught: unknown) => caught)) as ApiError;

		expect(error.message).toBe('입력한 내용을 확인해 주세요.');
		expect(error.fieldErrors).toEqual(['too short', 'digit']);
		expect(fetchFn).toHaveBeenCalledTimes(1);
	});
});

describe('logout', () => {
	it('posts to the logout endpoint', async () => {
		const fetchFn = vi.fn(async () => new Response(null, { status: 204 }));

		await logout(fetchFn);

		expect(fetchFn).toHaveBeenCalledWith('/api/auth/logout', { method: 'POST', headers: undefined, body: undefined });
	});
});

describe('email confirmation', () => {
	it('confirms with the numeric user id from the link and can resend the mail', async () => {
		const fetchFn = vi.fn<typeof fetch>(async () => new Response(null, { status: 204 }));

		await confirmEmail('42', 'code-1', fetchFn);
		await resendConfirmation(user.email, fetchFn);

		expect(fetchFn.mock.calls.map(([url]) => url)).toEqual(['/api/auth/email/confirm', '/api/auth/email/resend']);
		expect(fetchFn.mock.calls[0]?.[1]?.body).toBe(JSON.stringify({ userId: 42, code: 'code-1' }));
	});

	it('explains a social login to an unconfirmed account', () => {
		expect(externalLoginMessage('EMAIL_NOT_CONFIRMED_ERROR', 'Google')).toContain('이메일 확인');
	});
});

describe('password reset', () => {
	it('requests a link and then resets the password', async () => {
		const fetchFn = vi.fn<typeof fetch>(async () => new Response(null, { status: 204 }));

		await forgotPassword(user.email, fetchFn);
		await resetPassword(user.email, 'code-1', 'N3w-Passw0rd!', fetchFn);

		expect(fetchFn.mock.calls.map(([url]) => url)).toEqual(['/api/auth/password/forgot', '/api/auth/password/reset']);
		expect(fetchFn.mock.calls[1]?.[1]?.body).toBe(
			JSON.stringify({ email: user.email, code: 'code-1', newPassword: 'N3w-Passw0rd!' })
		);
	});
});

describe('social accounts and consent', () => {
	it('unlinks with DELETE and reports the last-method conflict', async () => {
		const fetchFn = vi.fn<typeof fetch>(async () =>
			Response.json({ code: 'LAST_LOGIN_METHOD_ERROR', detail: 'last' }, { status: 409 })
		);

		const error = (await unlink('Google', fetchFn).catch((caught: unknown) => caught)) as ApiError;

		expect(fetchFn).toHaveBeenCalledWith('/api/auth/external/Google', { method: 'DELETE' });
		expect(error.code).toBe('LAST_LOGIN_METHOD_ERROR');
	});

	it('reads consent details for the client and scopes', async () => {
		const fetchFn = vi.fn<typeof fetch>(async () =>
			Response.json({ clientId: 'sample-app', displayName: 'Sample App', scopes: [] })
		);

		const details = await getConsent('sample-app', 'openid email', fetchFn);

		expect(fetchFn).toHaveBeenCalledWith('/api/oauth/consent?client_id=sample-app&scope=openid%20email');
		expect(details.displayName).toBe('Sample App');
	});

	it('sends server paths through a full page navigation and explains results', () => {
		expect(isServerPath('/connect/authorize?client_id=a')).toBe(true);
		expect(isServerPath('/products')).toBe(false);
		expect(externalLoginMessage('EXTERNAL_LOGIN_NOT_LINKED_ERROR', 'Kakao')).toContain('아직 연결하지 않은 카카오 계정');
		expect(externalLoginMessage(null, null)).toBeNull();
	});
});

describe('safeReturnUrl', () => {
	it.each([
		['/products?page=2', '/products?page=2'],
		[null, '/'],
		['', '/'],
		['https://evil.example', '/'],
		['//evil.example', '/'],
		['/\\evil.example', '/']
	])('maps %s to %s', (value, expected) => {
		expect(safeReturnUrl(value)).toBe(expected);
	});
});
