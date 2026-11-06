import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router } from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { AuthService } from '../services/api-services/auth-service';
import { globalGuard } from './global-guard';

describe('Authentication guard', () => {
  it('checks authentication without requesting admin status', async () => {
    const auth = { isAuthenticated: vi.fn(() => of(true)), updateIsAdmin: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: { parseUrl: vi.fn() } },
      ],
    });
    const result = TestBed.runInInjectionContext(() =>
      globalGuard({}, [], new ActivatedRouteSnapshot()),
    );
    expect(await firstValueFrom(result as Observable<boolean>)).toBe(true);
    expect(auth.isAuthenticated).toHaveBeenCalledOnce();
    expect(auth.updateIsAdmin).not.toHaveBeenCalled();
  });

  it('redirects failed authentication to login', async () => {
    const login = {};
    const router = { parseUrl: vi.fn(() => login) };
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: { isAuthenticated: () => throwError(() => new Error('Unauthorized')) },
        },
        { provide: Router, useValue: router },
      ],
    });
    const result = TestBed.runInInjectionContext(() =>
      globalGuard({}, [], new ActivatedRouteSnapshot()),
    );
    expect(await firstValueFrom(result as Observable<unknown>)).toBe(login);
    expect(router.parseUrl).toHaveBeenCalledWith('/login');
  });
});
