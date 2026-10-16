import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ApiError } from './api-error';

export interface CurrentUser {
  id: number;
  email: string;
  permissions: string[];
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

  /** 가입한 뒤 같은 자격 증명으로 바로 로그인한다. */
  async register(email: string, password: string): Promise<CurrentUser | null> {
    await this.post('/api/auth/register', { email, password });
    return this.login(email, password);
  }

  async logout(): Promise<void> {
    await this.post('/api/auth/logout');
    this.user.set(null);
  }

  private async post(url: string, body?: object): Promise<void> {
    try {
      await firstValueFrom(this.http.post(url, body ?? null));
    } catch (error) {
      throw ApiError.from(error);
    }
  }
}
