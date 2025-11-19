import { Routes } from '@angular/router';
import { MsalGuard } from '@azure/msal-angular';

import { LoginComponent } from '../login/login';
import { AppComponent } from './app';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
  },

  {
    path: '',
    canActivate: [MsalGuard],
    component: AppComponent,
  },

  {
    path: '**',
    redirectTo: 'login',
  },
];
