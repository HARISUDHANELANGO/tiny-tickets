import { Injectable } from '@angular/core';
import { msalInstance } from '../config/msal.config';

@Injectable({ providedIn: 'root' })
export class AuthService {
  login() {
    msalInstance.loginRedirect({
      scopes: [
        'openid',
        'profile',
        'email',
        'api://f2cea967-6192-44ae-aedc-1e6b6a994e5e/access_as_user',
      ],
    });
  }

  logout() {
    msalInstance.logoutRedirect();
  }

  getAccount() {
    return msalInstance.getActiveAccount();
  }

  async getToken(scopes: string[]) {
    const acc = this.getAccount()!;
    return msalInstance.acquireTokenSilent({ account: acc, scopes });
  }
}
