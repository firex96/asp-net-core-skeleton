import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ApiError, safeReturnUrl } from '../auth/api-error';
import { AuthService, ExternalProvider } from '../auth/auth.service';
import { externalLoginMessage, isServerPath } from '../auth/navigation';

/**
 * 로그인과 가입을 한 화면에서 전환한다. 성공하면 returnUrl(같은 출처 경로만)로 돌아간다 — OAuth 서버의 authorize 같은 서버 주소면
 * 브라우저 이동으로 간다. 이미 로그인돼 있으면 곧바로 돌아간다. 소셜 로그인은 공급자마다 폼 POST 로 시작한다(연결된 계정만).
 */
@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
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
  readonly providers = signal<ExternalProvider[]>([]);
  readonly notice = signal<string | null>(null);

  email = '';
  password = '';
  confirmPassword = '';

  get returnUrl(): string {
    return safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl'));
  }

  async ngOnInit() {
    const query = this.route.snapshot.queryParamMap;
    this.notice.set(externalLoginMessage(query.get('error'), query.get('provider'))
      ?? (query.get('reset') === 'done' ? 'Your password was changed. Log in with the new password.' : null));
    const user = this.auth.user() === undefined ? await this.auth.refresh() : this.auth.user();
    if (user) {
      await this.continue();
      return;
    }

    this.providers.set(await this.auth.listProviders().catch(() => []));
  }

  externalLoginAction(provider: string): string {
    return `/api/auth/external/${encodeURIComponent(provider)}/login?returnUrl=${encodeURIComponent(this.returnUrl)}`;
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

  private async continue() {
    const target = this.returnUrl;
    if (isServerPath(target)) {
      window.location.assign(target);
      return;
    }

    await this.router.navigateByUrl(target);
  }
}
