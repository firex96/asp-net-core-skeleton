import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { MANAGE_PERMISSION } from './admin/admin.service';
import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.Eager
})
export class AppComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly managePermission = MANAGE_PERMISSION;
  private readonly router = inject(Router);
  /** 현재 사용자를 못 읽으면 로그인 버튼을 보인다 — 세션 값은 그대로 둬서(미확인) 화면들이 다시 묻게 한다. */
  readonly sessionFailed = signal(false);
  readonly logoutFailed = signal(false);

  ngOnInit() {
    this.auth.refresh().catch(error => {
      console.error(error);
      this.sessionFailed.set(true);
    });
  }

  /** 로그인 화면에서는 상단 로그인 버튼을 감춘다. */
  get onLoginPage(): boolean {
    return this.router.url.split('?')[0] === '/login';
  }

  /** 계정 화면들(로그인 · 비밀번호 찾기 · 재설정 · 이메일 확인)에서는 돌아올 곳을 싣지 않는다 — 링크의 일회용 코드를 다른 주소로 옮기지 않는다. */
  get loginQueryParams() {
    const path = this.router.url.split('?')[0];
    return ['/login', '/forgot-password', '/reset-password', '/confirm-email'].includes(path) ? {} : { returnUrl: this.router.url };
  }

  async logout() {
    this.logoutFailed.set(false);
    try {
      await this.auth.logout();
    } catch {
      this.logoutFailed.set(true);
      return;
    }
    await this.router.navigateByUrl('/');
  }
}
