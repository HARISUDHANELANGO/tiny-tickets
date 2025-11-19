import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export function authGuard() {
  const auth = inject(AuthService);

  const account = auth.getAccount();

  if (account) {
    return true;
  }

  // 🔥 Trigger MSAL login (NOT router navigation)
  auth.login();

  // 🔥 Stop activation — navigation will be taken over by MSAL
  return false;
}
