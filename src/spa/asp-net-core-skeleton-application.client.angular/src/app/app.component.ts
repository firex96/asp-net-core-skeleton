import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.Eager
})
export class AppComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  ngOnInit() {
    this.auth.refresh().catch(error => console.error(error));
  }

  /** 로그인 화면에서는 상단 로그인 버튼을 감춘다. */
  get onLoginPage(): boolean {
    return this.router.url.split('?')[0] === '/login';
  }

  /** 계정 화면들(로그인 · 비밀번호 찾기 · 재설정)에서는 돌아올 곳을 싣지 않는다 — 재설정 주소의 일회용 코드를 다른 주소로 옮기지 않는다. */
  get loginQueryParams() {
    const path = this.router.url.split('?')[0];
    return ['/login', '/forgot-password', '/reset-password'].includes(path) ? {} : { returnUrl: this.router.url };
  }

  async logout() {
    await this.auth.logout();
    await this.router.navigateByUrl('/');
  }
}
