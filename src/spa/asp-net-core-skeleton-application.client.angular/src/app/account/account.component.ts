import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ApiError } from '../auth/api-error';
import { AuthService, ExternalLogin, ExternalProvider } from '../auth/auth.service';
import { externalLoginMessage } from '../auth/navigation';

/**
 * 내 계정 — 권한과 연결된 소셜 계정. 연결은 공급자마다 폼 POST 로 시작하고, 공급자에서 돌아오면 <c>?linked=</c> · <c>?error=</c> 로 결과를 받는다.
 * 로그인하지 않았으면 로그인 화면으로 보낸다.
 */
@Component({
  selector: 'app-account',
  template: `
    <h1>Account</h1>
    @if (auth.user(); as user) {
      <p>{{ user.email }}</p>
      <h2>Permissions</h2>
      @if (user.permissions.length === 0) {
        <p>None</p>
      } @else {
        <ul>
          @for (permission of user.permissions; track permission) {
            <li><code>{{ permission }}</code></li>
          }
        </ul>
      }

      <h2>Linked accounts</h2>
      @if (notice(); as notice) {
        <p class="notice" role="status">{{ notice }}</p>
      }
      @if (error(); as error) {
        <p class="error" role="alert">{{ error.message }}</p>
      }
      <ul class="providers">
        @for (provider of providers(); track provider.name) {
          <li>
            <span>{{ provider.displayName }}</span>
            @if (isLinked(provider.name)) {
              <button type="button" (click)="unlink(provider.name)">Unlink</button>
            } @else {
              <form method="post" [attr.action]="linkAction(provider.name)">
                <button type="submit">Link</button>
              </form>
            }
          </li>
        }
      </ul>
    }
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: '../login/login.component.css'
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
    this.notice.set(linked ? `Your ${linked} account is linked. You can now log in with it.` : externalLoginMessage(query.get('error'), query.get('provider')));
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
