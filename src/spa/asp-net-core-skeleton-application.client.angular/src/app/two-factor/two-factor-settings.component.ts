import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ApiError } from '../auth/api-error';
import { AuthService, AuthenticatorSetup, TwoFactorStatus } from '../auth/auth.service';

/**
 * 내 계정의 2단계 인증 — 인증 앱(QR 등록 → 코드 확인) 또는 메일 코드로 켜고, 켤 때 받은 복구 코드는 이 화면에서 한 번만 보여 준다.
 * 끄려면 비밀번호를 다시 넣는다. 수단을 바꾸려면 끈 뒤에 다시 켠다.
 */
@Component({
  selector: 'app-two-factor-settings',
  imports: [FormsModule],
  template: `
    <h2 class="section-title">2단계 인증</h2>

    @if (recoveryCodes(); as codes) {
      <div class="alert alert-info" role="status">
        <p>복구 코드예요. 인증 앱이나 메일을 쓸 수 없을 때 하나씩 쓸 수 있어요. 이 화면을 떠나면 다시 볼 수 없으니 안전한 곳에 적어 두세요.</p>
        <ul class="codes">
          @for (recoveryCode of codes; track recoveryCode) {
            <li><code>{{ recoveryCode }}</code></li>
          }
        </ul>
      </div>
    }

    @if (error(); as error) {
      <p class="alert alert-error" role="alert">{{ error.message }}</p>
    }

    @if (status(); as status) {
      @if (!status.enabled) {
        <p class="muted">로그인할 때 비밀번호 다음에 확인 코드를 한 번 더 넣어요.</p>
        @if (setup(); as pending) {
          <p>인증 앱(Google Authenticator, Microsoft Authenticator 등)으로 QR 코드를 찍거나 키를 직접 입력해 주세요.</p>
          <img class="qr" [src]="pending.qrCodeDataUri" alt="인증 앱 등록 QR 코드" />
          <p class="muted">키 <code data-testid="shared-key">{{ pending.sharedKey }}</code></p>
          <form class="form" (ngSubmit)="confirmAuthenticator()">
            <label class="field">
              인증 앱의 6자리 코드
              <input name="code" autocomplete="one-time-code" inputmode="numeric" required [(ngModel)]="code" />
            </label>
            <div class="button-row">
              <button type="submit" class="btn btn-primary" [disabled]="busy()">확인하고 켜기</button>
              <button type="button" class="btn" (click)="cancelSetup()">취소</button>
            </div>
          </form>
        } @else {
          <div class="button-row">
            <button type="button" class="btn btn-primary btn-small" [disabled]="busy()" (click)="startAuthenticator()">인증 앱으로 켜기</button>
            <button type="button" class="btn btn-small" [disabled]="busy()" (click)="enableEmail()">메일 코드로 켜기</button>
          </div>
        }
      } @else {
        <p>켜져 있어요 — {{ status.method === 'authenticator' ? '인증 앱' : '메일 코드' }}로 확인해요. 남은 복구 코드는 {{ status.recoveryCodesLeft }}개예요.</p>
        @if (status.browserRemembered) {
          <p class="muted">이 브라우저는 기억돼 있어서 확인 코드를 묻지 않아요.</p>
        }
        <div class="button-row">
          <button type="button" class="btn btn-small" [disabled]="busy()" (click)="newRecoveryCodes()">복구 코드 새로 받기</button>
          @if (status.browserRemembered) {
            <button type="button" class="btn btn-small" [disabled]="busy()" (click)="forget()">이 브라우저 기억 지우기</button>
          }
        </div>
        <form class="form" (ngSubmit)="disable()">
          <label class="field">
            비밀번호
            <input name="password" type="password" autocomplete="current-password" required [(ngModel)]="password" />
          </label>
          <button type="submit" class="btn" [disabled]="busy()">2단계 인증 끄기</button>
        </form>
      }
    }
  `,
  // 카드의 직계 자식이 아니라서 공통 CSS 의 .card > .alert 간격이 걸리지 않는다 — 같은 간격을 여기서 준다.
  styles: [':host { display: block; } .alert { margin-bottom: 1rem; }'],
  changeDetection: ChangeDetectionStrategy.Eager
})
export class TwoFactorSettingsComponent implements OnInit {
  private readonly auth = inject(AuthService);

  readonly status = signal<TwoFactorStatus | null>(null);
  readonly setup = signal<AuthenticatorSetup | null>(null);
  readonly recoveryCodes = signal<string[] | null>(null);
  readonly error = signal<ApiError | null>(null);
  readonly busy = signal(false);
  code = '';
  password = '';

  async ngOnInit() {
    await this.load();
  }

  startAuthenticator() {
    return this.run(async () => {
      this.recoveryCodes.set(null);
      this.code = '';
      this.setup.set(await this.auth.setupAuthenticator());
    });
  }

  confirmAuthenticator() {
    return this.run(async () => {
      this.recoveryCodes.set(await this.auth.verifyAuthenticator(this.code));
      this.setup.set(null);
    });
  }

  cancelSetup() {
    this.setup.set(null);
  }

  enableEmail() {
    return this.run(async () => this.recoveryCodes.set(await this.auth.enableEmailTwoFactor()));
  }

  newRecoveryCodes() {
    return this.run(async () => this.recoveryCodes.set(await this.auth.regenerateRecoveryCodes()));
  }

  forget() {
    return this.run(() => this.auth.forgetBrowser());
  }

  disable() {
    return this.run(async () => {
      await this.auth.disableTwoFactor(this.password);
      this.password = '';
      this.recoveryCodes.set(null);
    });
  }

  private async load() {
    this.status.set(await this.auth.twoFactorStatus());
  }

  private async run(action: () => Promise<void>) {
    this.busy.set(true);
    this.error.set(null);
    try {
      await action();
      await this.load();
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
    } finally {
      this.busy.set(false);
    }
  }
}
