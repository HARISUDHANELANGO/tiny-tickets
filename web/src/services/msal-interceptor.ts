import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from './auth.service';
import { protectedResources } from '../config/msal.config';
import { from, switchMap } from 'rxjs';

export const msalInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  const isApiCall = req.url.startsWith(
    protectedResources.tinyTicketsApi.endpoint
  );

  if (!isApiCall) return next(req);

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
