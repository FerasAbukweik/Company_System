import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../services/client/auth-service';

export const adminGuard: CanMatchFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (await authService.isAdmin()) return true;

  router.navigateByUrl('/dashboard');
  return false;
};
