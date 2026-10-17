import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ApiError } from '../auth/api-error';
import { AuthService, Passkey } from '../auth/auth.service';
import { createPasskey, isPasskeyCancelled, isPasskeySupported } from '../auth/webauthn';

/** 내 계정의 패스키 — 이 기기(지문 · 얼굴 · PIN)나 보안 키로 비밀번호 없이 로그인한다. 등록은 브라우저의 패스키 창을 거친다. */
@Component({
  selector: 'app-passkey-settings',
  imports: [FormsModule],
  template: `
    <h2 class="section-title">패스키</h2>
    <p class="muted">패스키를 등록하면 비밀번호 없이 이 기기의 지문 · 얼굴 · PIN 으로 로그인할 수 있어요.</p>

    @if (notice(); as notice) {
      <p class="alert alert-info" role="status">{{ notice }}</p>
    }
    @if (error(); as error) {
      <p class="alert alert-error" role="alert">{{ error.message }}</p>
    }

    @if (passkeys().length > 0) {
      <ul class="list">
        @for (passkey of passkeys(); track passkey.id) {
          <li>
            <span>
              <strong>{{ passkey.name }}</strong>
              <span class="muted"> · {{ registeredOn(passkey) }} 등록</span>
            </span>
            <button type="button" class="btn btn-small" (click)="remove(passkey)">지우기</button>
          </li>
        }
      </ul>
    }

    @if (supported()) {
      <form class="form" (ngSubmit)="add()">
        <label class="field">
          패스키 이름
          <input name="passkeyName" placeholder="예: 회사 노트북" maxlength="50" [(ngModel)]="name" />
        </label>
        <button type="submit" class="btn btn-primary" [disabled]="busy()">패스키 추가</button>
      </form>
    } @else {
      <p class="muted">이 브라우저는 패스키를 지원하지 않아요.</p>
    }
  `,
  // 카드의 직계 자식이 아니라서 공통 CSS 의 .card > .alert 간격이 걸리지 않는다 — 같은 간격을 여기서 준다.
  styles: [':host { display: block; } .alert { margin-bottom: 1rem; }'],
  changeDetection: ChangeDetectionStrategy.Eager
})
export class PasskeySettingsComponent implements OnInit {
  private readonly auth = inject(AuthService);

  readonly passkeys = signal<Passkey[]>([]);
  readonly supported = signal(false);
  readonly error = signal<ApiError | null>(null);
  readonly notice = signal<string | null>(null);
  readonly busy = signal(false);
  name = '';

  async ngOnInit() {
    this.supported.set(isPasskeySupported());
    try {
      this.passkeys.set(await this.auth.listPasskeys());
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
    }
  }

  registeredOn(passkey: Passkey): string {
    return new Date(passkey.createdAt).toLocaleDateString('ko-KR');
  }

  async add() {
    this.busy.set(true);
    this.error.set(null);
    this.notice.set(null);
    try {
      const created = await this.auth.addPasskey(await createPasskey(await this.auth.passkeyCreationOptions()), this.name);
      this.notice.set(`패스키 '${created.name}'를 등록했어요. 다음부터 비밀번호 없이 로그인할 수 있어요.`);
      this.name = '';
      this.passkeys.set(await this.auth.listPasskeys());
    } catch (error) {
      this.error.set(isPasskeyCancelled(error)
        ? new ApiError(0, undefined, '패스키 등록을 취소했어요.')
        : error instanceof ApiError ? error : ApiError.from(error));
    } finally {
      this.busy.set(false);
    }
  }

  async remove(passkey: Passkey) {
    this.error.set(null);
    this.notice.set(null);
    try {
      await this.auth.removePasskey(passkey.id);
      this.passkeys.set(await this.auth.listPasskeys());
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
    }
  }
}
