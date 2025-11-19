import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app/app';
import { routes } from './app/app.routes';
import { appConfig } from './app/app.config';
import { msalInstance } from './config/msal.config';

async function start() {
  await msalInstance.initialize();

  const result = await msalInstance.handleRedirectPromise();
  if (result?.account) {
    msalInstance.setActiveAccount(result.account);
  }

  bootstrapApplication(AppComponent, {
    providers: [
      provideHttpClient(),
      provideRouter(routes),
      ...appConfig.providers,
    ],
  });
}

start();
