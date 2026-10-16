import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { ApiError } from '../auth/api-error';
import { AuthService } from '../auth/auth.service';

/** 메일 링크(<c>/confirm-email?userId=…&amp;code=…</c>)로 들어와 이메일을 확인한다. 로그인은 확인한 뒤 따로 한다. */
@Component({
  selector: 'app-confirm-email',
  imports: [RouterLink],
  template: `
    <main class="card">
      <h1>이메일 확인</h1>
      @switch (status()) {
        @case ('pending') {
          <p class="muted">확인하는 중이에요…</p>
        }
        @case ('done') {
          <p class="alert alert-info" role="status">이메일을 확인했어요. 이제 로그인할 수 있어요.</p>
          <a routerLink="/login" class="btn btn-primary btn-block">로그인하기</a>
        }
        @case ('failed') {
          <p class="alert alert-error" role="alert">{{ error()?.message }}</p>
          <p class="links"><a routerLink="/login">로그인으로 돌아가기</a></p>
        }
      }
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.Eager
})
export class ConfirmEmailComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly status = signal<'pending' | 'done' | 'failed'>('pending');
  readonly error = signal<ApiError | null>(null);

  async ngOnInit() {
    const query = this.route.snapshot.queryParamMap;
    try {
      await this.auth.confirmEmail(query.get('userId') ?? '', query.get('code') ?? '');
      this.status.set('done');
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
      this.status.set('failed');
    }
  }
}
