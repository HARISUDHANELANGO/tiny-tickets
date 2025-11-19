import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import {
  PublicClientApplication,
  AuthenticationResult,
  AccountInfo,
} from '@azure/msal-browser';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.html',
  styleUrls: ['./login.scss'],
})
export class LoginComponent {
  private msal = new PublicClientApplication({
    auth: {
      clientId: 'd754916c-6408-4ecd-9dfd-68d64993ecae',
      authority:
        'https://login.microsoftonline.com/0784cbd8-29cf-49ad-ae0e-4ebd47bf32d1',
      redirectUri: 'https://salmon-rock-08a479500.3.azurestaticapps.net',
    },
  });

  user: AccountInfo | null = null;

  constructor() {
    // Detect redirect callback
    this.msal.handleRedirectPromise().then((result) => {
      if (result?.account) {
        this.msal.setActiveAccount(result.account);
        this.user = result.account;
      } else {
        this.user = this.msal.getActiveAccount();
      }
    });
  }

  login() {
    this.msal.loginRedirect({
      scopes: ['User.Read'],
    });
  }

  logout() {
    this.msal.logoutRedirect();
  }
}
