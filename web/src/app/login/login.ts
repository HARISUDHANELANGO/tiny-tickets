import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { msalInstance } from '../../config/msal.config';

@Component({
  selector: 'app-login',
  standalone: true,
  templateUrl: './login.html',
  styleUrls: ['./login.scss'],
})
export class LoginComponent {
  constructor(private auth: AuthService) {
    msalInstance.handleRedirectPromise().then((result) => {
      if (result?.account) {
        msalInstance.setActiveAccount(result.account);
      }
    });
  }

  login() {
    this.auth.login();
  }
}
