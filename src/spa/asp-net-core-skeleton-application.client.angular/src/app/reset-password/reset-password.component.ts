import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ApiError } from '../auth/api-error';
import { AuthService } from '../auth/auth.service';

/** 메일 링크(<c>/reset-password?email=…&amp;code=…</c>)로 들어와 새 비밀번호를 정한다. 성공하면 로그인 화면으로 간다. */
@Component({
  selector: 'app-reset-password',
  imports: [FormsModule, RouterLink],
  template: `
    <main class="card">
      <h1>새 비밀번호 정하기</h1>
      <p class="lead">{{ email }} 계정에서 쓸 새 비밀번호를 입력해 주세요.</p>
      <form class="form" (ngSubmit)="submit()">
        <label class="field">
          새 비밀번호
          <input name="password" type="password" autocomplete="new-password" required [(ngModel)]="password" />
        </label>
        <label class="field">
          새 비밀번호 확인
          <input name="confirmPassword" type="password" autocomplete="new-password" required [(ngModel)]="confirmPassword" />
        </label>
        <p class="hint">10자 이상, 영문 대문자 · 소문자와 숫자, 특수 문자를 섞어 주세요.</p>
        @if (error(); as error) {
          <div class="alert alert-error" role="alert">
            <p>{{ error.message }}</p>
            @if (error.fieldErrors.length > 0) {
              <ul>
                @for (message of error.fieldErrors; track message) {
                  <li>{{ message }}</li>
                }
              </ul>
            }
            @if (error.code === 'INVALID_TOKEN_ERROR') {
              <p><a routerLink="/forgot-password">재설정 링크 다시 받기</a></p>
            }
          </div>
        }
        <button type="submit" class="btn btn-primary btn-block" [disabled]="busy()">비밀번호 바꾸기</button>
      </form>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.Eager
})
export class ResetPasswordComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly busy = signal(false);
  readonly error = signal<ApiError | null>(null);
  readonly email = this.route.snapshot.queryParamMap.get('email') ?? '';
  private readonly code = this.route.snapshot.queryParamMap.get('code') ?? '';
  password = '';
  confirmPassword = '';

  async submit() {
    if (this.password !== this.confirmPassword) {
      this.error.set(new ApiError(400, undefined, '비밀번호가 서로 달라요. 다시 입력해 주세요.'));
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    try {
      await this.auth.resetPassword(this.email, this.code, this.password);
      await this.router.navigateByUrl('/login?reset=done');
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
    } finally {
      this.busy.set(false);
    }
  }
}
