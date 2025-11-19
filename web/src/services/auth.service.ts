import { Injectable } from '@angular/core';
import { msalInstance } from '../config/msal.config';

@Injectable({ providedIn: 'root' })
export class AuthService {
  login() {
    return msalInstance.loginRedirect({
      scopes: ['User.Read'],
    });
  }

  logout() {
    return msalInstance.logoutRedirect();
  }

  getAccount() {
    const accounts = msalInstance.getAllAccounts();
    return accounts.length ? accounts[0] : undefined;
  }

  async getToken(scopes: string[]) {
    const account = this.getAccount();

    return msalInstance.acquireTokenSilent({
      account: account,
      scopes: scopes,
    });
  }

  isLoggedIn(): boolean {
    return !!this.getAccount();
  }
}
