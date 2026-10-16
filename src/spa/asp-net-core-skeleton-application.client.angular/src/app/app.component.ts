import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  ngOnInit() {
    this.auth.refresh().catch(error => console.error(error));
  }

  get loginQueryParams() {
    return this.router.url.startsWith('/login') ? {} : { returnUrl: this.router.url };
  }

  async logout() {
    await this.auth.logout();
    await this.router.navigateByUrl('/');
  }
}
