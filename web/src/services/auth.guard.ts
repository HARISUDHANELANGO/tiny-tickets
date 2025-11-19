import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { msalInstance } from '../config/msal.config';

export function authGuard() {
  const auth = inject(AuthService);
  const router = inject(Router);

  // 🔥 DO NOT LOGIN while MSAL is handling redirect
  const interactionStatus = sessionStorage.getItem('msal.interaction.status');
  if (interactionStatus === 'interaction_in_progress') {
    return false;
  }

  if (auth.isLoggedIn()) return true;

  auth.login();
  return false;
}
