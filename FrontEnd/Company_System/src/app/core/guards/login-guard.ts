import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../services/api-services/auth-service';

export const loginGuard: CanMatchFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  return authService.isAuthenticated(true).pipe(
    map(() => router.parseUrl('/dashboard')),
    catchError(() => of(true)),
  );
};
