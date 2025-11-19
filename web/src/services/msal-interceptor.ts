import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { from, switchMap } from 'rxjs';
import { HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { protectedResources } from '../config/msal.config';

export const msalInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  // ---------------------------------------------
  // 1. Skip adding token for Microsoft auth URLs
  // ---------------------------------------------
  if (req.url.includes('login.microsoftonline.com')) {
    return next(req);
  }

  // ---------------------------------------------
  // 2. Only add token for our backend API
  // ---------------------------------------------
  const isApiRequest = req.url.startsWith(
    protectedResources.tinyTicketsApi.endpoint
  );

  if (!isApiRequest) {
    return next(req);
  }

  // ---------------------------------------------
  // 3. Acquire token silently and attach
  // ---------------------------------------------
  return from(auth.getToken(protectedResources.tinyTicketsApi.scopes)).pipe(
    switchMap((result) => {
      if (result?.accessToken) {
        const cloned = req.clone({
          setHeaders: {
            Authorization: `Bearer ${result.accessToken}`,
          },
        });
        return next(cloned);
      }

      return next(req);
    })
  );
};
