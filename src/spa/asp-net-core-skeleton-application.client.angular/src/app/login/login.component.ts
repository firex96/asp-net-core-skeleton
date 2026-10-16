import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ApiError, safeReturnUrl } from '../auth/api-error';
import { AuthService, ExternalProvider, TwoFactorProvider } from '../auth/auth.service';
import { externalLoginMessage, isServerPath } from '../auth/navigation';
import { TwoFactorStepComponent } from '../two-factor/two-factor-step.component';

/**
 * 로그인과 가입을 한 화면에서 전환한다. 로그인에 성공하면 returnUrl(같은 출처 경로만)로 돌아간다 — OAuth 서버의 authorize 같은 서버 주소면
 * 브라우저 이동으로 간다. 이미 로그인돼 있으면 곧바로 돌아간다. 가입하면 확인 메일 안내를 보이고 로그인 화면으로 돌아온다(이메일을 확인해야
 * 로그인할 수 있다). 2단계 인증을 켠 계정은 두 번째 단계(TwoFactorStepComponent)를 거친다 — 소셜 로그인 뒤에는 서버가
 * /login?step=two-factor 로 보낸다. 소셜 로그인은 공급자마다 폼 POST 로 시작한다(연결된 계정만).
 */
@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, TwoFactorStepComponent],
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.Eager
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
  readonly twoFactor = signal<TwoFactorProvider[] | null>(null);

  email = '';
  password = '';
  confirmPassword = '';

  get returnUrl(): string {
    return safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl'));
  }

  async ngOnInit() {
    const query = this.route.snapshot.queryParamMap;
    this.notice.set(externalLoginMessage(query.get('error'), query.get('provider'))
      ?? (query.get('reset') === 'done' ? '비밀번호를 바꿨어요. 새 비밀번호로 로그인해 주세요.' : null));
    const user = this.auth.user() === undefined ? await this.auth.refresh() : this.auth.user();
    if (user) {
      await this.continue();
      return;
    }

    if (query.get('step') === 'two-factor') {
      this.twoFactor.set(await this.auth.twoFactorChallenge().catch(() => null));
      if (!this.twoFactor()) {
        this.notice.set('로그인 시간이 지났어요. 처음부터 다시 로그인해 주세요.');
      }
    }

    this.providers.set(await this.auth.listProviders().catch(() => []));
  }

  externalLoginAction(provider: string): string {
    return `/api/auth/external/${encodeURIComponent(provider)}/login?returnUrl=${encodeURIComponent(this.returnUrl)}`;
  }

  toggleMode() {
    this.mode.update(mode => (mode === 'login' ? 'register' : 'login'));
    this.error.set(null);
    this.notice.set(null);
  }

  async submit() {
    if (this.mode() === 'register' && this.password !== this.confirmPassword) {
      this.error.set(new ApiError(400, undefined, '비밀번호가 서로 달라요. 다시 입력해 주세요.'));
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.notice.set(null);
    try {
      if (this.mode() === 'login') {
        this.twoFactor.set(await this.auth.login(this.email, this.password));
        if (!this.twoFactor()) {
          await this.continue();
        }
      } else {
        await this.auth.register(this.email, this.password);
        this.mode.set('login');
        this.password = '';
        this.confirmPassword = '';
        this.notice.set(`${this.email}로 확인 메일을 보냈어요. 메일의 링크를 열어 이메일을 확인한 뒤 로그인해 주세요.`);
      }
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
    } finally {
      this.busy.set(false);
    }
  }

  restart() {
    this.twoFactor.set(null);
    this.password = '';
    this.notice.set(null);
  }

  async resend() {
    this.error.set(null);
    try {
      await this.auth.resendConfirmation(this.email);
      this.notice.set(`${this.email}로 확인 메일을 다시 보냈어요. 메일함을 확인해 주세요.`);
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
    }
  }

  async continue() {
    const target = this.returnUrl;
    if (isServerPath(target)) {
      window.location.assign(target);
      return;
    }

    await this.router.navigateByUrl(target);
  }
}
