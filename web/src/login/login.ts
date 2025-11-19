import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import {
  PublicClientApplication,
  AuthenticationResult,
  AccountInfo,
} from '@azure/msal-browser';
import { msalInstance } from '../config/msal.config';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.html',
  styleUrls: ['./login.scss'],
})
export class LoginComponent {
  private msal = msalInstance;

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
