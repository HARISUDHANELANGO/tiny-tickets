import { Injectable } from '@angular/core';
import { msalInstance, protectedResources } from '../config/msal.config';

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
    const accts = msalInstance.getAllAccounts();
    return accts.length ? accts[0] : undefined;
  }

  async getToken(scopes: string[]) {
    const acct = this.getAccount();
    if (!acct) return this.login();

    // silent first
    try {
      return await msalInstance.acquireTokenSilent({
        account: acct,
        scopes,
      });
    } catch {
      // fallback redirect
      return msalInstance.acquireTokenRedirect({ scopes });
    }
  }

  isLoggedIn() {
    return !!this.getAccount();
  }
}
