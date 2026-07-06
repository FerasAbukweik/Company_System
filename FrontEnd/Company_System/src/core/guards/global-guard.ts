import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../services/api/Auth-api-service';
import { firstValueFrom } from 'rxjs';

export const globalGuard: CanMatchFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  try {
    await firstValueFrom(authService.isAuthenticated());

    return true;
  } catch (err) {
    router.navigateByUrl('/login');
    return false;
  }
};
