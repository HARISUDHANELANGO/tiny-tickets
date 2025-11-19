import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from './auth.service';
import { protectedResources } from '../config/msal.config';
import { from, switchMap } from 'rxjs';

export const msalInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  // Skip all token work during redirect
  if (
    sessionStorage.getItem('msal.interaction.status') ===
    'interaction_in_progress'
  ) {
    return next(req);
  }

  const isApiCall = req.url.startsWith(
    protectedResources.tinyTicketsApi.endpoint
  );

  if (!isApiCall) return next(req);

  return from(auth.getToken(protectedResources.tinyTicketsApi.scopes)).pipe(
    switchMap((result) => {
      if (result?.accessToken) {
        return next(
          req.clone({
            setHeaders: {
              Authorization: `Bearer ${result.accessToken}`,
            },
          })
        );
      }

      return next(req);
    })
  );
};
