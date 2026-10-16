import { HttpClient, HttpErrorResponse } from '@angular/common/http';
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

  async login(email: string, password: string): Promise<CurrentUser | null> {
    await this.post('/api/auth/login', { email, password });
    return this.refresh();
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
    try {
      await firstValueFrom(this.http.post(url, body ?? null));
    } catch (error) {
      throw ApiError.from(error);
    }
  }
}
