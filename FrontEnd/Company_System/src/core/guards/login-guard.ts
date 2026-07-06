import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../services/api/Auth-api-service';

export const loginGuard: CanMatchFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  authService.isAuthenticated().subscribe({
    next: () => {
      router.navigateByUrl('/dashboard');
    },
  });

  return true;
};
