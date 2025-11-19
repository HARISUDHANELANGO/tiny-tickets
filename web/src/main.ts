import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { routes } from './app/app.routes';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app';
import { msalInstance } from './config/msal.config';

// ------------------------------------------------------
// MSAL initialization wrapped in async IIFE
// ------------------------------------------------------
(async () => {
  await msalInstance.initialize();

  await msalInstance.handleRedirectPromise().then((result) => {
    if (result?.account) {
      msalInstance.setActiveAccount(result.account);
    }
  });

  bootstrapApplication(AppComponent, {
    providers: [
      provideHttpClient(),
      provideRouter(routes),
      ...appConfig.providers,
    ],
  }).catch((err) => console.error(err));
})();
