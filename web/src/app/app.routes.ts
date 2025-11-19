import { Routes } from '@angular/router';
import { LoginComponent } from './login/login';
import { authGuard } from '../services/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
  },

  {
    path: '',
    loadComponent: () => import('./home/home').then((m) => m.HomeComponent),
    canActivate: [authGuard],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
