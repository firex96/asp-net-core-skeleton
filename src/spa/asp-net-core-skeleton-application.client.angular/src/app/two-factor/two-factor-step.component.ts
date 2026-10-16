import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ApiError } from '../auth/api-error';
import { AuthService, TwoFactorProvider } from '../auth/auth.service';

/**
 * 로그인 두 번째 단계 — 계정의 수단(인증 앱 또는 메일 코드)으로 확인하고, 그 수단을 쓸 수 없으면 복구 코드로 한다. 메일 코드는 이 단계에
 * 들어오면 한 번 보내고, 다시 받을 수 있다. <c>&lt;main class="card" appTwoFactorStep&gt;</c> 로 쓴다 — 내용이 카드의 직계 자식이 되게.
 */
@Component({
  selector: 'main[appTwoFactorStep]',
  imports: [FormsModule],
  template: `
    <h1>2단계 인증</h1>
    <p class="lead">{{ lead() }}</p>

    @if (notice(); as notice) {
      <p class="alert alert-info" role="status">{{ notice }}</p>
    }

    <form class="form" (ngSubmit)="submit()">
      <label class="field">
        {{ method() === 'recovery' ? '복구 코드' : '확인 코드' }}
        <input name="code" autocomplete="one-time-code" [attr.inputmode]="method() === 'recovery' ? 'text' : 'numeric'" required
               [(ngModel)]="code" />
      </label>
      @if (method() !== 'recovery') {
        <label class="checkbox">
          <input name="rememberBrowser" type="checkbox" [(ngModel)]="rememberBrowser" />
          이 브라우저 기억하기
        </label>
      }

      @if (error(); as error) {
        <p class="alert alert-error" role="alert">{{ error.message }}</p>
      }

      <button type="submit" class="btn btn-primary btn-block" [disabled]="busy()">확인</button>
    </form>

    <p class="links">
      @if (method() === 'recovery') {
        @if (primary() !== 'recovery') {
          <button type="button" class="link-button" (click)="choose(primary())">
            {{ primary() === 'authenticator' ? '인증 앱 코드로 확인하기' : '메일 코드로 확인하기' }}
          </button>
        }
      } @else {
        @if (method() === 'email') {
          <button type="button" class="link-button" (click)="sendCode(true)">코드 다시 받기</button>
        }
        @if (providers().includes('recovery')) {
          <button type="button" class="link-button" (click)="choose('recovery')">복구 코드로 확인하기</button>
        }
      }
      <button type="button" class="link-button" (click)="restart.emit()">처음부터 다시 로그인</button>
    </p>
  `,
  changeDetection: ChangeDetectionStrategy.Eager
})
export class TwoFactorStepComponent implements OnInit {
  private readonly auth = inject(AuthService);

  readonly providers = input.required<TwoFactorProvider[]>();
  readonly done = output<void>();
  readonly restart = output<void>();

  readonly primary = computed<TwoFactorProvider>(() => this.providers().find(provider => provider !== 'recovery') ?? 'recovery');
  private readonly chosen = signal<TwoFactorProvider | null>(null);
  readonly method = computed(() => this.chosen() ?? this.primary());
  readonly lead = computed(() => {
    switch (this.method()) {
      case 'authenticator':
        return '인증 앱에 보이는 6자리 코드를 입력해 주세요.';
      case 'email':
        return '메일로 받은 6자리 코드를 입력해 주세요.';
      default:
        return '복구 코드 하나를 입력해 주세요. 한 번 쓴 코드는 다시 쓸 수 없어요.';
    }
  });
  readonly error = signal<ApiError | null>(null);
  readonly notice = signal<string | null>(null);
  readonly busy = signal(false);
  code = '';
  rememberBrowser = false;

  ngOnInit() {
    if (this.primary() === 'email') {
      void this.sendCode(false);
    }
  }

  async sendCode(again: boolean) {
    this.error.set(null);
    try {
      await this.auth.sendTwoFactorEmail();
      this.notice.set(again ? '확인 코드를 다시 보냈어요. 메일함을 확인해 주세요.' : '메일로 확인 코드를 보냈어요. 메일함을 확인해 주세요.');
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
    }
  }

  choose(next: TwoFactorProvider) {
    this.chosen.set(next);
    this.code = '';
    this.error.set(null);
    this.notice.set(null);
  }

  async submit() {
    this.busy.set(true);
    this.error.set(null);
    try {
      await this.auth.completeTwoFactor(this.method(), this.code, this.method() !== 'recovery' && this.rememberBrowser);
      this.done.emit();
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
    } finally {
      this.busy.set(false);
    }
  }
}
