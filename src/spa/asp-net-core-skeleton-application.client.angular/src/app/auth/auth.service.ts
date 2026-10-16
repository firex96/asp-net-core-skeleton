import { HttpClient, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ApiError } from './api-error';

export interface CurrentUser {
  id: number;
  email: string;
  permissions: string[];
}

export interface ExternalProvider {
  name: string;
  displayName: string;
}

export interface ExternalLogin {
  provider: string;
  displayName: string;
}

/** 로그인 두 번째 단계의 수단 — 계정마다 인증 앱 또는 메일 코드 하나, 남은 복구 코드가 있으면 recovery 도. */
export type TwoFactorProvider = 'authenticator' | 'email' | 'recovery';

export interface TwoFactorStatus {
  enabled: boolean;
  method: 'authenticator' | 'email' | null;
  recoveryCodesLeft: number;
  browserRemembered: boolean;
}

export interface AuthenticatorSetup {
  sharedKey: string;
  authenticatorUri: string;
  qrCodeDataUri: string;
}

interface TwoFactorChallenge {
  providers: TwoFactorProvider[];
}

interface RecoveryCodes {
  recoveryCodes: string[];
}

export interface ConsentDetails {
  clientId: string;
  displayName: string;
  scopes: { name: string; description: string }[];
}

/**
 * 쿠키 세션 로그인. 세션 쿠키(HttpOnly)는 브라우저가 들고 다니므로 이 서비스는 토큰을 저장하지 않는다 —
 * 로그인 상태는 /api/auth/me 로만 확인한다.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  /** undefined = 아직 확인 전, null = 로그인 안 함. */
  readonly user = signal<CurrentUser | null | undefined>(undefined);

  async refresh(): Promise<CurrentUser | null> {
    try {
      const user = await firstValueFrom(this.http.get<CurrentUser>('/api/auth/me'));
      this.user.set(user);
      return user;
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        this.user.set(null);
        return null;
      }
      throw ApiError.from(error);
    }
  }

  /** 로그인한다. 2단계 인증을 켠 계정이면 두 번째 단계의 수단을 돌려주고 로그인 상태는 그대로 둔다(두 번째 단계는 completeTwoFactor). */
  async login(email: string, password: string): Promise<TwoFactorProvider[] | null> {
    const response = await this.postForResponse<TwoFactorChallenge>('/api/auth/login', { email, password });
    if (response.status === 202) {
      return response.body?.providers ?? [];
    }

    await this.refresh();
    return null;
  }

  /** 두 번째 단계를 기다리는 중이면 그 수단들(소셜 로그인 뒤 /login?step=two-factor 로 왔을 때). 아니면 null. */
  async twoFactorChallenge(): Promise<TwoFactorProvider[] | null> {
    try {
      return (await firstValueFrom(this.http.get<TwoFactorChallenge>('/api/auth/login/2fa'))).providers;
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        return null;
      }
      throw ApiError.from(error);
    }
  }

  /** 로그인 확인 코드를 메일로 받는다(메일 코드를 쓰는 계정). */
  sendTwoFactorEmail(): Promise<void> {
    return this.post('/api/auth/login/2fa/email');
  }

  /** 두 번째 단계 — 맞으면 로그인된다. rememberBrowser 면 이 브라우저에서는 다음부터 묻지 않는다. */
  async completeTwoFactor(provider: TwoFactorProvider, code: string, rememberBrowser: boolean): Promise<void> {
    await this.post('/api/auth/login/2fa', { provider, code, rememberBrowser });
    await this.refresh();
  }

  twoFactorStatus(): Promise<TwoFactorStatus> {
    return this.get<TwoFactorStatus>('/api/account/two-factor');
  }

  /** 인증 앱 등록을 시작한다 — 새 키와 QR 코드. 코드를 확인해야 켜진다. */
  async setupAuthenticator(): Promise<AuthenticatorSetup> {
    return (await this.postForResponse<AuthenticatorSetup>('/api/account/two-factor/authenticator/setup')).body!;
  }

  /** 인증 앱의 코드로 2단계 인증을 켠다. 복구 코드를 돌려준다(이때 한 번만 받는다). */
  async verifyAuthenticator(code: string): Promise<string[]> {
    return (await this.postForResponse<RecoveryCodes>('/api/account/two-factor/authenticator/verify', { code })).body!.recoveryCodes;
  }

  /** 메일 코드로 2단계 인증을 켠다. 복구 코드를 돌려준다. */
  async enableEmailTwoFactor(): Promise<string[]> {
    return (await this.postForResponse<RecoveryCodes>('/api/account/two-factor/email/enable')).body!.recoveryCodes;
  }

  /** 복구 코드를 새로 받는다 — 전에 받은 코드는 더 쓰지 못한다. */
  async regenerateRecoveryCodes(): Promise<string[]> {
    return (await this.postForResponse<RecoveryCodes>('/api/account/two-factor/recovery-codes')).body!.recoveryCodes;
  }

  disableTwoFactor(password: string): Promise<void> {
    return this.post('/api/account/two-factor/disable', { password });
  }

  forgetBrowser(): Promise<void> {
    return this.post('/api/account/two-factor/forget-browser');
  }

  /** 계정을 만든다. 로그인하지 않는다 — 서버가 보낸 메일의 링크로 이메일을 확인해야 로그인할 수 있다. */
  register(email: string, password: string): Promise<void> {
    return this.post('/api/auth/register', { email, password });
  }

  /** 메일 링크(/confirm-email?userId=…&code=…)의 값으로 이메일을 확인한다. */
  confirmEmail(userId: string, code: string): Promise<void> {
    return this.post('/api/auth/email/confirm', { userId: Number(userId), code });
  }

  /** 확인 메일을 다시 받는다. 계정이 있든 없든 성공한다 — 서버가 가입 여부를 드러내지 않는다. */
  resendConfirmation(email: string): Promise<void> {
    return this.post('/api/auth/email/resend', { email });
  }

  async logout(): Promise<void> {
    await this.post('/api/auth/logout');
    this.user.set(null);
  }

  /** 계정이 있든 없든 성공한다 — 서버가 가입 여부를 드러내지 않는다. */
  forgotPassword(email: string): Promise<void> {
    return this.post('/api/auth/password/forgot', { email });
  }

  resetPassword(email: string, code: string, newPassword: string): Promise<void> {
    return this.post('/api/auth/password/reset', { email, code, newPassword });
  }

  listProviders(): Promise<ExternalProvider[]> {
    return this.get<ExternalProvider[]>('/api/auth/external/providers');
  }

  listLogins(): Promise<ExternalLogin[]> {
    return this.get<ExternalLogin[]>('/api/auth/external/logins');
  }

  async unlink(provider: string): Promise<void> {
    try {
      await firstValueFrom(this.http.delete(`/api/auth/external/${encodeURIComponent(provider)}`));
    } catch (error) {
      throw ApiError.from(error);
    }
  }

  consent(clientId: string, scope: string): Promise<ConsentDetails> {
    return this.get<ConsentDetails>(`/api/oauth/consent?client_id=${encodeURIComponent(clientId)}&scope=${encodeURIComponent(scope)}`);
  }

  private async get<T>(url: string): Promise<T> {
    try {
      return await firstValueFrom(this.http.get<T>(url));
    } catch (error) {
      throw ApiError.from(error);
    }
  }

  private async post(url: string, body?: object): Promise<void> {
    await this.postForResponse(url, body);
  }

  private async postForResponse<T>(url: string, body?: object): Promise<HttpResponse<T>> {
    try {
      return await firstValueFrom(this.http.post<T>(url, body ?? null, { observe: 'response' }));
    } catch (error) {
      throw ApiError.from(error);
    }
  }
}
