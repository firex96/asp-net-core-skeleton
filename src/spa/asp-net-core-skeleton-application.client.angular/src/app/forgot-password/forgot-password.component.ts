import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { ApiError } from '../auth/api-error';
import { AuthService } from '../auth/auth.service';

/** 재설정 링크를 메일로 받는다. 계정이 있든 없든 같은 안내를 보인다(가입 여부를 드러내지 않는다). */
@Component({
  selector: 'app-forgot-password',
  imports: [FormsModule, RouterLink],
  template: `
    <main class="card">
      <h1>비밀번호 찾기</h1>
      @if (sent()) {
        <p class="alert alert-info" role="status">
          {{ email }}로 가입한 계정이 있다면 비밀번호 재설정 링크를 보냈어요. 메일함을 확인해 주세요. 링크는 2시간 동안 한 번만 쓸 수 있어요.
        </p>
      } @else {
        <p class="lead">가입한 이메일을 입력하면 비밀번호를 다시 정할 수 있는 링크를 보내 드려요.</p>
        <form class="form" (ngSubmit)="submit()">
          <label class="field">
            이메일
            <input name="email" type="email" autocomplete="username" placeholder="name@example.com" required [(ngModel)]="email" />
          </label>
          @if (error(); as error) {
            <p class="alert alert-error" role="alert">{{ error.message }}</p>
          }
          <button type="submit" class="btn btn-primary btn-block" [disabled]="busy()">재설정 링크 받기</button>
        </form>
      }
      <p class="links"><a routerLink="/login">로그인으로 돌아가기</a></p>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.Eager
})
export class ForgotPasswordComponent {
  private readonly auth = inject(AuthService);

  readonly sent = signal(false);
  readonly busy = signal(false);
  readonly error = signal<ApiError | null>(null);
  email = '';

  async submit() {
    this.busy.set(true);
    this.error.set(null);
    try {
      await this.auth.forgotPassword(this.email);
      this.sent.set(true);
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
    } finally {
      this.busy.set(false);
    }
  }
}
