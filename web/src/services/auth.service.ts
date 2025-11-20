import { Injectable } from '@angular/core';
import { msalInstance } from '../config/msal.config';

@Injectable({ providedIn: 'root' })
export class AuthService {
  login() {
    return msalInstance.loginRedirect({
      scopes: ['api://9768865e-83af-4a37-8785-b3a547a4e220/access_as_user'],
    });
  }

  logout() {
    msalInstance.logoutRedirect();
  }

  getAccount() {
    return msalInstance.getActiveAccount();
  }

  async getToken() {
    const account = msalInstance.getActiveAccount()!;
    return msalInstance.acquireTokenSilent({
      account,
      scopes: ['api://9768865e-83af-4a37-8785-b3a547a4e220/access_as_user'],
    });
  }
}
