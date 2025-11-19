import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app/app';
import { routes } from './app/app.routes';
import { appConfig } from './app/app.config';
import { msalInstance } from './config/msal.config';

// 🔥 MSAL v3 requires async initialization before any MSAL API call
msalInstance.initialize().then(() => {
  bootstrapApplication(AppComponent, {
    providers: [
      provideHttpClient(),
      provideRouter(routes),
      ...appConfig.providers,
    ],
  }).catch((err) => console.error(err));
});
