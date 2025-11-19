import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { msalInstance } from '../config/msal.config';

export function authGuard() {
  const auth = inject(AuthService);
  const router = inject(Router);

  // prevent guard from triggering login during redirect
  if (
    sessionStorage.getItem('msal.interaction.status') ===
    'interaction_in_progress'
  ) {
    return false;
  }

  if (auth.isLoggedIn()) {
    return true;
  }

  auth.login();
  return false;
}
