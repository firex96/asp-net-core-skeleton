import { Routes } from '@angular/router';

import { AccountComponent } from './account/account.component';
import { ConsentComponent } from './consent/consent.component';
import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { LoginComponent } from './login/login.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';
import { WeatherComponent } from './weather/weather.component';

export const routes: Routes = [
  { path: '', component: WeatherComponent, title: '날씨 예보' },
  { path: 'login', component: LoginComponent, title: '로그인' },
  { path: 'forgot-password', component: ForgotPasswordComponent, title: '비밀번호 찾기' },
  { path: 'reset-password', component: ResetPasswordComponent, title: '비밀번호 재설정' },
  { path: 'account', component: AccountComponent, title: '내 계정' },
  { path: 'consent', component: ConsentComponent, title: '접근 허용' },
  { path: '**', redirectTo: '' }
];
