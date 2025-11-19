import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap } from 'rxjs';
import { AuthService } from './auth.service';
import { protectedResources } from '../config/msal.config';

export const msalInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  return from(auth.getToken(protectedResources.tinyTicketsApi.scopes)).pipe(
    switchMap((res) => {
      if (res?.accessToken) {
        req = req.clone({
          setHeaders: {
            Authorization: `Bearer ${res.accessToken}`,
          },
        });
      }
      return next(req);
    })
  );
};
