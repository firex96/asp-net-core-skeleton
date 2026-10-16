import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ApiError, safeReturnUrl } from './api-error';
import { AuthService } from './auth.service';
import { externalLoginMessage, isServerPath } from './navigation';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const user = { id: 7, email: 'user@example.com', permissions: [] };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('treats 401 from /api/auth/me as signed out', async () => {
    const result = service.refresh();
    httpMock.expectOne('/api/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(await result).toBeNull();
    expect(service.user()).toBeNull();
  });

  it('logs in and then loads the current user', async () => {
    const result = service.login(user.email, 'Passw0rd!');

    const login = httpMock.expectOne('/api/auth/login');
    expect(login.request.method).toEqual('POST');
    expect(login.request.body).toEqual({ email: user.email, password: 'Passw0rd!' });
    login.flush(null, { status: 204, statusText: 'No Content' });
    await new Promise(resolve => setTimeout(resolve));
    httpMock.expectOne('/api/auth/me').flush(user);

    expect(await result).toBeNull();
    expect(service.user()).toEqual(user);
  });

  it('returns the second-step providers without signing in when the server answers 202', async () => {
    const result = service.login(user.email, 'Passw0rd!');
    httpMock.expectOne('/api/auth/login').flush({ providers: ['authenticator', 'recovery'] }, { status: 202, statusText: 'Accepted' });

    expect(await result).toEqual(['authenticator', 'recovery']);
    httpMock.expectNone('/api/auth/me');
  });

  it('completes the second step and then loads the current user', async () => {
    const result = service.completeTwoFactor('email', '123456', true);
    const request = httpMock.expectOne('/api/auth/login/2fa');
    expect(request.request.body).toEqual({ provider: 'email', code: '123456', rememberBrowser: true });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await new Promise(resolve => setTimeout(resolve));
    httpMock.expectOne('/api/auth/me').flush(user);
    await result;

    expect(service.user()).toEqual(user);
  });

  it('treats a missing pending sign-in as null', async () => {
    const result = service.twoFactorChallenge();
    httpMock.expectOne('/api/auth/login/2fa').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(await result).toBeNull();
  });

  it('reads the authenticator setup', async () => {
    const setup = { sharedKey: 'abcd efgh', authenticatorUri: 'otpauth://totp/x', qrCodeDataUri: 'data:image/svg+xml;base64,' };
    const result = service.setupAuthenticator();
    httpMock.expectOne('/api/account/two-factor/authenticator/setup').flush(setup);

    expect(await result).toEqual(setup);
  });

  it('turns a problem response into ApiError with the server message', async () => {
    const result = service.login(user.email, 'wrong');
    httpMock.expectOne('/api/auth/login').flush(
      { status: 423, code: 'ACCOUNT_LOCKED_ERROR', detail: 'locked' },
      { status: 423, statusText: 'Locked' });

    const error = await rejection(result);
    expect(error).toBeInstanceOf(ApiError);
    expect(error.message).toBe('locked');
    expect(error.status).toBe(423);
    expect(error.code).toBe('ACCOUNT_LOCKED_ERROR');
  });

  it('collects validation messages from errors', async () => {
    const result = service.register(user.email, 'short');
    httpMock.expectOne('/api/auth/register').flush(
      { title: 'One or more validation errors occurred.', errors: { PasswordTooShort: ['too short'], PasswordRequiresDigit: ['digit'] } },
      { status: 400, statusText: 'Bad Request' });

    const error = await rejection(result);
    expect(error.message).toBe('입력한 내용을 확인해 주세요.');
    expect(error.fieldErrors).toEqual(['too short', 'digit']);
  });

  it('only creates the account on register — signing in waits for the email confirmation', async () => {
    const result = service.register(user.email, 'Passw0rd!');
    httpMock.expectOne('/api/auth/register').flush(null, { status: 201, statusText: 'Created' });

    await result;
    httpMock.expectNone('/api/auth/login');
  });

  it('confirms the email with the numeric user id and can resend the mail', async () => {
    const confirm = service.confirmEmail('42', 'code-1');
    const confirmRequest = httpMock.expectOne('/api/auth/email/confirm');
    expect(confirmRequest.request.body).toEqual({ userId: 42, code: 'code-1' });
    confirmRequest.flush(null, { status: 204, statusText: 'No Content' });
    await confirm;

    const resend = service.resendConfirmation(user.email);
    httpMock.expectOne('/api/auth/email/resend').flush(null, { status: 202, statusText: 'Accepted' });
    await resend;
  });

  it('requests a reset link and resets the password', async () => {
    const forgot = service.forgotPassword(user.email);
    const forgotRequest = httpMock.expectOne('/api/auth/password/forgot');
    expect(forgotRequest.request.body).toEqual({ email: user.email });
    forgotRequest.flush(null, { status: 202, statusText: 'Accepted' });
    await forgot;

    const reset = service.resetPassword(user.email, 'code-1', 'N3w-Passw0rd!');
    const resetRequest = httpMock.expectOne('/api/auth/password/reset');
    expect(resetRequest.request.body).toEqual({ email: user.email, code: 'code-1', newPassword: 'N3w-Passw0rd!' });
    resetRequest.flush(null, { status: 204, statusText: 'No Content' });
    await reset;
  });

  it('unlinks a social account with DELETE', async () => {
    const result = service.unlink('Google');
    const request = httpMock.expectOne('/api/auth/external/Google');
    expect(request.request.method).toEqual('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await result;
  });

  it('reads consent details for the client and scopes', async () => {
    const result = service.consent('sample-app', 'openid email');
    httpMock.expectOne('/api/oauth/consent?client_id=sample-app&scope=openid%20email')
      .flush({ clientId: 'sample-app', displayName: 'Sample App', scopes: [] });

    expect((await result).displayName).toBe('Sample App');
  });

  it('clears the user on logout', async () => {
    service.user.set(user);
    const result = service.logout();
    httpMock.expectOne('/api/auth/logout').flush(null, { status: 204, statusText: 'No Content' });
    await result;

    expect(service.user()).toBeNull();
  });
});

async function rejection(promise: Promise<unknown>): Promise<ApiError> {
  try {
    await promise;
  } catch (error) {
    return error as ApiError;
  }
  throw new Error('expected the request to fail');
}

describe('navigation', () => {
  it('sends server paths through a full page navigation', () => {
    expect(isServerPath('/connect/authorize?client_id=a')).toBe(true);
    expect(isServerPath('/products')).toBe(false);
  });

  it('explains social login results', () => {
    expect(externalLoginMessage('EXTERNAL_LOGIN_IN_USE_ERROR', 'Naver')).toBe('이 네이버 계정은 이미 다른 회원과 연결돼 있어요.');
    expect(externalLoginMessage(null, null)).toBeNull();
    expect(externalLoginMessage('EMAIL_NOT_CONFIRMED_ERROR', 'Google')).toContain('이메일 확인');
  });
});

describe('safeReturnUrl', () => {
  it.each([
    ['/products?page=2', '/products?page=2'],
    [null, '/'],
    ['', '/'],
    ['https://evil.example', '/'],
    ['//evil.example', '/'],
    ['/\\evil.example', '/'],
  ])('maps %s to %s', (value, expected) => {
    expect(safeReturnUrl(value)).toBe(expected);
  });
});
