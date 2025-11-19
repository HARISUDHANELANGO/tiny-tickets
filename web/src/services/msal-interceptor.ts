import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { from, switchMap } from 'rxjs';
import { HttpInterceptorFn } from '@angular/common/http';

export const msalInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  return from(
    auth.getToken(['api://f2cea967-6192-44ae-aedc-1e6b6a994e5e'])
  ).pipe(
    switchMap((result: any) => {
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
