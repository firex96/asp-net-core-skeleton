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
    <h1>Reset password</h1>
    <p>For {{ email }}</p>
    <form (ngSubmit)="submit()">
      <label>
        New password
        <input name="password" type="password" autocomplete="new-password" required [(ngModel)]="password" />
      </label>
      <label>
        Confirm new password
        <input name="confirmPassword" type="password" autocomplete="new-password" required [(ngModel)]="confirmPassword" />
      </label>
      @if (error(); as error) {
        <div class="error" role="alert">
          <p>{{ error.message }}</p>
          @if (error.fieldErrors.length > 0) {
            <ul>
              @for (message of error.fieldErrors; track message) {
                <li>{{ message }}</li>
              }
            </ul>
          }
          @if (error.code === 'INVALID_TOKEN_ERROR') {
            <p><a routerLink="/forgot-password">Request a new link</a></p>
          }
        </div>
      }
      <button type="submit" [disabled]="busy()">Change password</button>
    </form>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: '../login/login.component.css'
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
      this.error.set(new ApiError(400, undefined, 'Passwords do not match.'));
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
