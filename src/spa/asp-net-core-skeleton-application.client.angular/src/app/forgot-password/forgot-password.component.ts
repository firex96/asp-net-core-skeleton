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
    <h1>Forgot password</h1>
    @if (sent()) {
      <p role="status">If an account exists for {{ email }}, we sent a link to reset the password. The link works once within 2 hours.</p>
    } @else {
      <form (ngSubmit)="submit()">
        <label>
          Email
          <input name="email" type="email" autocomplete="username" required [(ngModel)]="email" />
        </label>
        @if (error(); as error) {
          <p class="error" role="alert">{{ error.message }}</p>
        }
        <button type="submit" [disabled]="busy()">Send reset link</button>
      </form>
    }
    <p><a routerLink="/login">Back to log in</a></p>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: '../login/login.component.css'
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
