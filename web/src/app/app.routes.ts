import { Routes } from '@angular/router';
import { LoginComponent } from '../login/login';
import { AppComponent } from './app';
import { authGuard } from '../services/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
  },

  {
    path: '',
    component: AppComponent,
    canActivate: [authGuard], // <–– NEW custom guard
  },

  {
    path: '**',
    redirectTo: 'login',
  },
];
