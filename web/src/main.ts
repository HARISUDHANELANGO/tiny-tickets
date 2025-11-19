import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app/app';
import { routes } from './app/app.routes';
import { appConfig } from './app/app.config';
import { msalInstance } from './config/msal.config';

// 🔥 1. Initialize MSAL first
msalInstance.initialize().then(() => {
  // 🔥 2. THEN bootstrap Angular
  bootstrapApplication(AppComponent, {
    providers: [
      provideHttpClient(),
      provideRouter(routes),
      ...appConfig.providers,
    ],
  }).catch((err) => console.error(err));
});
