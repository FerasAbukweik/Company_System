import { inject, Injectable, signal } from '@angular/core';
import { UserDTO } from '../../dto/user-dto';
import { firstValueFrom, tap } from 'rxjs';
import { LoginDTO } from '../../dto/login-dto';
import { AuthApiService } from '../api/Auth-api-service';
import { ToastService } from './toast-service';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  // DI
  private readonly authApiService = inject(AuthApiService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  // private
  private userData = signal<UserDTO | null>(null);
  private isUserAdmin = signal<boolean | null>(null);

  // getters
  get getUserData() {
    return this.userData.asReadonly();
  }

  get getIsAdmin() {
    return this.isUserAdmin.asReadonly();
  }

  // constructor
  constructor() {
    const localUser = localStorage.getItem('user');
    if (localUser) {
      this.userData.set(JSON.parse(localUser));
    }
  }

  // methods

  async isAdmin() {
    if (this.isUserAdmin() != null) return this.isUserAdmin();
    try {
      await firstValueFrom(this.authApiService.isAdmin());
      this.isUserAdmin.set(true);
      return true;
    } catch {
      this.isUserAdmin.set(false);
      return false;
    }
  }

  async isAuthenticated() {
    if (this.userData()) return true;
    try {
      await firstValueFrom(this.authApiService.isAuthenticated());
      return true;
    } catch {
      return false;
    }
  }

  logout() {
    this.authApiService.logout().subscribe({
      next: () => {
        this.router.navigateByUrl('/login');
        localStorage.removeItem('user');
        window.location.reload();
      },
      error: () => {
        this.toastService.error('something went wrong logging out');
      },
    });
  }

  login(loginData: LoginDTO) {
    return this.authApiService.login(loginData).pipe(
      tap((userData) => {
        // update is admin
        this.isAdmin();
        this.userData.set(userData);
        localStorage.setItem('user', JSON.stringify(userData));

        this.router.navigateByUrl('/dashboard');
      }),
    );
  }
}
