import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../services/api-services/auth-service';

export const adminGuard: CanMatchFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  return authService.updateIsAdmin().pipe(
    map((isAdmin) => isAdmin || router.parseUrl('/dashboard')),
    catchError((error) => of(router.parseUrl(error.status === 401 ? '/login' : '/dashboard'))),
  );
};
