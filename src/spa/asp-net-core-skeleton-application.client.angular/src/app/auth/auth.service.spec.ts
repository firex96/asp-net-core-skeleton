import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ApiError, safeReturnUrl } from './api-error';
import { AuthService } from './auth.service';

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

    expect(await result).toEqual(user);
    expect(service.user()).toEqual(user);
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
    expect(error.message).toBe('One or more validation errors occurred.');
    expect(error.fieldErrors).toEqual(['too short', 'digit']);
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
