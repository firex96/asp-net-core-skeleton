import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { ApiError, safeReturnUrl } from '../auth/api-error';
import { AuthService } from '../auth/auth.service';

/** 로그인과 가입을 한 화면에서 전환한다. 성공하면 returnUrl(같은 출처 경로만)로 돌아간다. */
@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly mode = signal<'login' | 'register'>('login');
  readonly error = signal<ApiError | null>(null);
  readonly busy = signal(false);

  email = '';
  password = '';
  confirmPassword = '';

  async ngOnInit() {
    const user = this.auth.user() === undefined ? await this.auth.refresh() : this.auth.user();
    if (user) {
      await this.continue();
    }
  }

  toggleMode() {
    this.mode.update(mode => (mode === 'login' ? 'register' : 'login'));
    this.error.set(null);
  }

  async submit() {
    if (this.mode() === 'register' && this.password !== this.confirmPassword) {
      this.error.set(new ApiError(400, undefined, 'Passwords do not match.'));
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    try {
      if (this.mode() === 'login') {
        await this.auth.login(this.email, this.password);
      } else {
        await this.auth.register(this.email, this.password);
      }
      await this.continue();
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
    } finally {
      this.busy.set(false);
    }
  }

  private continue() {
    return this.router.navigateByUrl(safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl')));
  }
}
