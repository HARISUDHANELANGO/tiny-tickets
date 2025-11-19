import { Injectable } from '@angular/core';
import { msalInstance, protectedResources } from '../config/msal.config';
import { AuthenticationResult } from '@azure/msal-browser';

@Injectable({ providedIn: 'root' })
export class AuthService {
  login() {
    return msalInstance.loginRedirect({
      scopes: ['User.Read', ...protectedResources.tinyTicketsApi.scopes],
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

    if (!account) {
      // Not logged in → start login
      return msalInstance.loginRedirect({ scopes });
    }

    try {
      // Try silent token
      return await msalInstance.acquireTokenSilent({
        account,
        scopes,
      });
    } catch (e) {
      // Silent failed → go interactive
      return msalInstance.acquireTokenRedirect({
        scopes,
      });
    }
  }

  isLoggedIn(): boolean {
    return !!this.getAccount();
  }
}
