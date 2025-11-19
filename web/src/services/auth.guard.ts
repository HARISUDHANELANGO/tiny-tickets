import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

export function authGuard() {
  const auth = inject(AuthService);
  const router = inject(Router);

  const account = auth.getAccount();

  if (account) {
    return true;
  }

  // Redirect to login if no session exists
  router.navigate(['/login']);
  return false;
}
