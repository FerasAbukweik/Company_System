import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../services/client/auth-service';

export const globalGuard: CanMatchFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  await authService.isAuthenticated();

  if (authService.getUserData()) return true;

  router.navigateByUrl('/login');
  return false;
};
