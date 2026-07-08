import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthApiService } from '../services/api/Auth-api-service';

export const loginGuard: CanMatchFn = () => {
  const authService = inject(AuthApiService);
  const router = inject(Router);

  authService.isAuthenticated().subscribe({
    next: () => {
      router.navigateByUrl('/dashboard');
    },
  });

  return true;
};
