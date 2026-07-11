import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../services/client/auth-service';

export const globalGuard: CanMatchFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (await authService.isAuthenticated()) {
    // update is admin
    authService.isAdmin();
    
    return true;
  }

  router.navigateByUrl('/login');
  return false;
};
