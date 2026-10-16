import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ApiError } from '../auth/api-error';
import { AuthService, ConsentDetails } from '../auth/auth.service';

/**
 * OAuth 동의 — 인가 서버가 <c>/consent?…(authorize 요청 그대로)</c> 로 보낸다. 허락 · 거절은 같은 요청을 <c>consent=accept|deny</c> 와 함께
 * <c>/connect/authorize</c> 로 폼 POST 해 인가 서버가 처리한다(허락은 저장돼 다시 묻지 않는다).
 */
@Component({
  selector: 'app-consent',
  template: `
    <main class="card">
      @if (details(); as details) {
        <h1>{{ details.displayName }}에 접근을 허용할까요?</h1>
        <p class="lead">허용하면 이 앱이 회원님 계정으로 아래 일을 할 수 있어요.</p>
        <ul class="list">
          @for (scope of details.scopes; track scope.name) {
            <li>
              <span>{{ scope.description }}</span>
              <code class="muted">{{ scope.name }}</code>
            </li>
          }
        </ul>
        <form method="post" action="/connect/authorize">
          @for (parameter of parameters; track parameter[0]) {
            <input type="hidden" [attr.name]="parameter[0]" [attr.value]="parameter[1]" />
          }
          <div class="actions">
            <button type="submit" name="consent" value="deny" class="btn">거부</button>
            <button type="submit" name="consent" value="accept" class="btn btn-primary">허용</button>
          </div>
        </form>
        <p class="hint">한 번 허용하면 다음부터는 묻지 않아요.</p>
      } @else if (!error()) {
        <p class="muted">불러오는 중이에요…</p>
      }
      @if (error(); as error) {
        <p class="alert alert-error" role="alert">{{ error.message }}</p>
      }
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.Eager
})
export class ConsentComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly details = signal<ConsentDetails | null>(null);
  readonly error = signal<ApiError | null>(null);
  readonly parameters: [string, string][] = this.route.snapshot.queryParamMap.keys
    .map(key => [key, this.route.snapshot.queryParamMap.get(key) ?? ''] as [string, string]);

  async ngOnInit() {
    const user = this.auth.user() === undefined ? await this.auth.refresh() : this.auth.user();
    if (!user) {
      await this.router.navigateByUrl(`/login?returnUrl=${encodeURIComponent(this.router.url)}`);
      return;
    }

    const query = this.route.snapshot.queryParamMap;
    try {
      this.details.set(await this.auth.consent(query.get('client_id') ?? '', query.get('scope') ?? ''));
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
    }
  }
}
