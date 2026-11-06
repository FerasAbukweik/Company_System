import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { vi } from 'vitest';
import { authInterceptor } from './auth-interceptor';
import { AuthApiService } from '../services/api/auth-api-service';
import { AuthService } from '../services/api-services/auth-service';
import { ToastService } from '../services/api-services/toast-service';
import { Urls } from '../constants/urls';

describe('Authentication recovery', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let auth: AuthService;
  const router = { navigateByUrl: vi.fn().mockResolvedValue(true) };
  const refreshUrl = `${Urls.api}/Auth/UpdateTokens`;

  beforeEach(() => {
    localStorage.clear();
    router.navigateByUrl.mockClear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
        { provide: ToastService, useValue: { error: vi.fn() } },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => controller.verify());

  it('shares refresh requests and retries both failed calls', () => {
    http.get('/first').subscribe();
    http.get('/second').subscribe();
    controller.expectOne('/first').flush(null, { status: 401, statusText: 'Unauthorized' });
    controller.expectOne('/second').flush(null, { status: 401, statusText: 'Unauthorized' });
    controller.expectOne(refreshUrl).flush(null);
    controller.expectOne('/first').flush({});
    controller.expectOne('/second').flush({});
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('ends the session when refresh fails without attempting another refresh', () => {
    localStorage.setItem('user', '{}');
    const error = vi.fn();
    http.get('/private').subscribe({ error });
    controller.expectOne('/private').flush(null, { status: 401, statusText: 'Unauthorized' });
    controller.expectOne(refreshUrl).flush(null, { status: 401, statusText: 'Unauthorized' });
    controller.expectNone(refreshUrl);
    expect(error).toHaveBeenCalledOnce();
    expect(localStorage.getItem('user')).toBeNull();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login', { replaceUrl: true });
  });

  it('keeps the session when a retried request has a server error', () => {
    http.get('/private').subscribe({ error: () => {} });
    controller.expectOne('/private').flush(null, { status: 401, statusText: 'Unauthorized' });
    controller.expectOne(refreshUrl).flush(null);
    controller.expectOne('/private').flush(null, { status: 500, statusText: 'Server Error' });
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('does not refresh a rejected login or a login-page session probe', () => {
    const api = TestBed.inject(AuthApiService);
    auth.isAuthenticated(true).subscribe({ error: () => {} });
    controller
      .expectOne(`${Urls.api}/Users/Me`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    api.login({ email: 'test@example.com', password: 'wrong' }).subscribe({ error: () => {} });
    controller
      .expectOne(`${Urls.api}/Auth/Login`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    controller.expectNone(refreshUrl);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('does not trust a stored profile as proof of authentication', () => {
    localStorage.setItem('user', '{}');
    auth.isAuthenticated().subscribe({ error: () => {} });
    controller.expectOne(`${Urls.api}/Users/Me`).flush({ userId: 'user' });
  });

  it('logs out successfully even when the access cookie has expired', () => {
    localStorage.setItem('user', '{}');
    auth.logout().subscribe();
    controller
      .expectOne(`${Urls.api}/Auth/Logout`)
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(auth.userData()).toBeNull();
    expect(auth.isAdmin()).toBeNull();
    expect(localStorage.getItem('user')).toBeNull();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login', { replaceUrl: true });
  });
});
