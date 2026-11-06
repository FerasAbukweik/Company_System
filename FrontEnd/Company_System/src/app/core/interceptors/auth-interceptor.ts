import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { SKIP_AUTH_RECOVERY } from '../constants/auth-http-context';
import { AuthService } from '../services/api-services/auth-service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const request = req.clone({ withCredentials: true });
  const router = inject(Router);
  const auth = inject(AuthService);

  const expireSession = () => {
    auth.clearSession();
    router.navigateByUrl('/login', { replaceUrl: true });
  };

  return next(request).pipe(
    catchError((error) => {
      if (error.status !== 401 || request.context.get(SKIP_AUTH_RECOVERY)) {
        return throwError(() => error);
      }

      return auth.updateTokens().pipe(
        catchError((refreshError) => {
          expireSession();
          return throwError(() => refreshError);
        }),
        switchMap(() =>
          next(request).pipe(
            catchError((retryError) => {
              if (retryError.status === 401) expireSession();
              return throwError(() => retryError);
            }),
          ),
        ),
      );
    }),
  );
};
