import { Routes } from '@angular/router';

import { AccountComponent } from './account/account.component';
import { ConsentComponent } from './consent/consent.component';
import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { LoginComponent } from './login/login.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';
import { WeatherComponent } from './weather/weather.component';

export const routes: Routes = [
  { path: '', component: WeatherComponent },
  { path: 'login', component: LoginComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  { path: 'account', component: AccountComponent },
  { path: 'consent', component: ConsentComponent },
  { path: '**', redirectTo: '' }
];
