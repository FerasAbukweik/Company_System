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
  private readonly _authApiService = inject(AuthApiService);
  private readonly _toastService = inject(ToastService);
  private readonly _router = inject(Router);

  // private
  private _userData = signal<UserDTO | null>(null);
  private _isUserAdmin = signal<boolean | null>(null);

  // getters
  get userData() {
    return this._userData.asReadonly();
  }

  get isAdmin() {
    return this._isUserAdmin.asReadonly();
  }

  // constructor
  constructor() {
    const localUser = localStorage.getItem('user');
    if (localUser) {
      this._userData.set(JSON.parse(localUser));
    }
  }

  // methods

  async updateIsAdmin() {
    if (this._isUserAdmin() != null) return this._isUserAdmin();
    try {
      await firstValueFrom(this._authApiService.isAdmin());
      this._isUserAdmin.set(true);
      return true;
    } catch {
      this._isUserAdmin.set(false);
      return false;
    }
  }

  async isAuthenticated() {
    if (this._userData()) return true;
    try {
      await firstValueFrom(this._authApiService.isAuthenticated());
      return true;
    } catch {
      return false;
    }
  }

  logout() {
    this._authApiService.logout().subscribe({
      next: () => {
        this._router.navigateByUrl('/login');
        localStorage.removeItem('user');
        window.location.reload();
      },
      error: () => {
        this._toastService.error('something went wrong logging out');
      },
    });
  }

  login(loginData: LoginDTO) {
    return this._authApiService.login(loginData).pipe(
      tap((userData) => {
        // update is admin
        this.isAdmin();
        this._userData.set(userData);
        localStorage.setItem('user', JSON.stringify(userData));

        this._router.navigateByUrl('/dashboard');
      }),
    );
  }
}
