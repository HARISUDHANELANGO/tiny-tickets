import {
  ApplicationConfig,
  provideZoneChangeDetection,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';

import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';

import { msalInstance } from '../config/msal.config';
import { msalInterceptor } from '../services/msal-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),

    // ✔ Router provided at the root
    provideRouter(routes),

    // ✔ HttpClient with MSAL token injection
    provideHttpClient(withInterceptors([msalInterceptor])),

    // ✔ Global MSAL Instance
    { provide: 'MSAL_INSTANCE', useValue: msalInstance },
  ],
};
