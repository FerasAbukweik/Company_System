import { ToastService } from './toast-service';
import { toastError } from '../../utils/toast-error';
import { inject, Injectable, Injector, signal } from '@angular/core';
import { UserDTO } from '../../dto/auth/user-dto';
import { catchError, defer, map, of, tap, throwError } from 'rxjs';
import { LoginDTO } from '../../dto/auth/login-dto';
import { AuthApiService } from '../api/auth-api-service';
import { Router } from '@angular/router';
import { UsersService } from './users-service';
import { PaymentsService } from './payments-service';
import { DailyWorkRecordsService } from './daily-work-records-service';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly _toastService = inject(ToastService);

  // DI
  private readonly _authApiService = inject(AuthApiService);
  private readonly _router = inject(Router);
  private readonly injector = inject(Injector);

  // private
  private readonly _userData = signal<UserDTO | null>(null);
  private readonly _isUserAdmin = signal<boolean | null>(null);

  constructor() {
    // Profiles are kept in memory; remove data persisted by earlier versions.
    localStorage.removeItem('user');
  }

  // getters
  get userData() {
    return this._userData.asReadonly();
  }

  get isAdmin() {
    return this._isUserAdmin.asReadonly();
  }

  // methods

  // makes sure isAdmin is not null
  updateIsAdmin() {
    return defer(() => {
      const cached = this._isUserAdmin();
      if (cached !== null) return of(cached);

      return this._authApiService.isAdmin().pipe(
        tap({
          next: () => this._isUserAdmin.set(true),
          error: (error) => this._isUserAdmin.set(error.status === 403 ? false : null),
        }),
        map(() => true),
      );
    });
  }

  updateTokens() {
    return this._authApiService.updateTokens();
  }

  isAuthenticated(skipRecovery = false) {
    return defer(() => {
      if (this._userData() !== null) return of(true);

      return this.injector
        .get(UsersService)
        .me(skipRecovery)
        .pipe(
          tap((user) => this._userData.set(user)),
          map(() => true),
        );
    });
  }

  logout() {
    return this._authApiService.logout().pipe(
      catchError((error) => (error.status === 401 ? of(undefined) : throwError(() => error))),
      toastError(this._toastService, 'تعذر تسجيل الخروج'),
      tap(() => {
        this.clearSession();
        this._router.navigateByUrl('/login', { replaceUrl: true });
      }),
    );
  }

  private resetDataCaches() {
    this.injector.get(UsersService).reset();
    this.injector.get(PaymentsService).reset();
    this.injector.get(DailyWorkRecordsService).reset();
  }

  clearSession() {
    this.resetDataCaches();
    this._userData.set(null);
    this._isUserAdmin.set(null);
    localStorage.removeItem('user');
  }

  login(loginData: LoginDTO) {
    return this._authApiService.login(loginData).pipe(
      toastError(this._toastService, 'تعذر تسجيل الدخول'),
      tap((userData) => {
        this.resetDataCaches();
        // update is admin
        this._isUserAdmin.set(null);
        this._userData.set(userData);
        this.injector.get(UsersService).cacheProfile(userData);

        this._router.navigateByUrl('/dashboard');
      }),
    );
  }
}
