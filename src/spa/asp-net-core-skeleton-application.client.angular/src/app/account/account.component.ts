import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ApiError } from '../auth/api-error';
import { AuthService, ExternalLogin, ExternalProvider } from '../auth/auth.service';
import { externalLoginMessage, providerName } from '../auth/navigation';

/**
 * 내 계정 — 권한과 연결된 소셜 계정. 연결은 공급자마다 폼 POST 로 시작하고, 공급자에서 돌아오면 <c>?linked=</c> · <c>?error=</c> 로 결과를 받는다.
 * 로그인하지 않았으면 로그인 화면으로 보낸다.
 */
@Component({
  selector: 'app-account',
  template: `
    <main class="card card-wide">
      <h1>내 계정</h1>
      @if (auth.user(); as user) {
        <p class="lead">{{ user.email }}</p>

        <h2 class="section-title">권한</h2>
        @if (user.permissions.length === 0) {
          <p class="muted">아직 받은 권한이 없어요.</p>
        } @else {
          <ul class="tags">
            @for (permission of user.permissions; track permission) {
              <li class="tag"><code>{{ permission }}</code></li>
            }
          </ul>
        }

        <h2 class="section-title">연결된 계정</h2>
        <p class="muted">연결해 두면 비밀번호 없이 그 계정으로 로그인할 수 있어요.</p>
        @if (notice(); as notice) {
          <p class="alert alert-info" role="status">{{ notice }}</p>
        }
        @if (error(); as error) {
          <p class="alert alert-error" role="alert">{{ error.message }}</p>
        }
        <ul class="list">
          @for (provider of providers(); track provider.name) {
            <li>
              <span>
                <strong>{{ provider.displayName }}</strong>
                <span class="muted"> · {{ isLinked(provider.name) ? '연결됨' : '연결 안 됨' }}</span>
              </span>
              @if (isLinked(provider.name)) {
                <button type="button" class="btn btn-small" (click)="unlink(provider.name)">연결 끊기</button>
              } @else {
                <form method="post" [attr.action]="linkAction(provider.name)">
                  <button type="submit" class="btn btn-primary btn-small">연결하기</button>
                </form>
              }
            </li>
          }
        </ul>
      }
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.Eager
})
export class AccountComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly providers = signal<ExternalProvider[]>([]);
  readonly logins = signal<ExternalLogin[]>([]);
  readonly notice = signal<string | null>(null);
  readonly error = signal<ApiError | null>(null);
  private readonly linked = computed(() => new Set(this.logins().map(login => login.provider)));

  async ngOnInit() {
    const user = this.auth.user() === undefined ? await this.auth.refresh() : this.auth.user();
    if (!user) {
      await this.router.navigateByUrl(`/login?returnUrl=${encodeURIComponent('/account')}`);
      return;
    }

    const query = this.route.snapshot.queryParamMap;
    const linked = query.get('linked');
    this.notice.set(linked
      ? `${providerName(linked)} 계정을 연결했어요. 이제 ${providerName(linked)}로 로그인할 수 있어요.`
      : externalLoginMessage(query.get('error'), query.get('provider')));
    await this.load();
  }

  isLinked(provider: string): boolean {
    return this.linked().has(provider);
  }

  linkAction(provider: string): string {
    return `/api/auth/external/${encodeURIComponent(provider)}/link`;
  }

  async unlink(provider: string) {
    this.error.set(null);
    this.notice.set(null);
    try {
      await this.auth.unlink(provider);
      await this.load();
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
    }
  }

  private async load() {
    const [providers, logins] = await Promise.all([this.auth.listProviders(), this.auth.listLogins()]);
    this.providers.set(providers);
    this.logins.set(logins);
  }
}
