import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { AuthService } from './auth-service';
import { AuthApiService } from '../api/auth-api-service';
import { ToastService } from './toast-service';

describe('Authentication state', () => {
  const api = { isAdmin: vi.fn(), login: vi.fn() };

  beforeEach(() => {
    localStorage.clear();
    api.login.mockReset();
    api.isAdmin.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthApiService, useValue: api },
        { provide: Router, useValue: { navigateByUrl: vi.fn() } },
        { provide: ToastService, useValue: { error: vi.fn() } },
      ],
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    localStorage.clear();
  });

  it('ignores malformed stored profile data instead of crashing startup', () => {
    localStorage.setItem('user', '{broken');
    const service = TestBed.inject(AuthService);
    expect(service.userData()).toBeNull();
    expect(localStorage.getItem('user')).toBeNull();
  });

  it('loads the user through Me once and reuses the stored user for later checks', () => {
    localStorage.setItem('user', JSON.stringify({ userId: 'old-session' }));
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);
    auth.isAuthenticated().subscribe();
    auth.isAuthenticated().subscribe();
    const request = http.expectOne((request) => request.url.endsWith('/Users/Me'));
    request.flush({ userId: 'current-user', fullName: 'Current Employee' });
    expect(auth.userData()?.userId).toBe('current-user');
    auth.isAuthenticated().subscribe();
    http.expectNone(() => true);
  });

  it('does not cache a failed Me response', () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);
    auth.isAuthenticated().subscribe({ error: () => {} });
    http
      .expectOne((request) => request.url.endsWith('/Users/Me'))
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(auth.userData()).toBeNull();
    auth.isAuthenticated().subscribe();
    http
      .expectOne((request) => request.url.endsWith('/Users/Me'))
      .flush({ userId: 'current-user' });
  });

  it('uses the profile returned by login without an extra Me request', () => {
    const auth = TestBed.inject(AuthService);
    api.login.mockReturnValue(of({ userId: 'current-user' }));
    auth.login({ email: 'employee@example.com', password: 'password' }).subscribe();
    auth.isAuthenticated().subscribe();
    TestBed.inject(HttpTestingController).expectNone(() => true);
    expect(auth.userData()?.userId).toBe('current-user');
  });

  it('does not permanently cache a temporary failure as a non-admin result', () => {
    const service = TestBed.inject(AuthService);
    api.isAdmin
      .mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 500 })))
      .mockReturnValue(of(undefined));
    service.updateIsAdmin().subscribe({ error: () => {} });
    expect(service.isAdmin()).toBeNull();
    service.updateIsAdmin().subscribe();
    expect(service.isAdmin()).toBe(true);
  });
  it('reuses cached admin status, including a non-admin result', () => {
    const service = TestBed.inject(AuthService);
    api.isAdmin.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 403 })));
    service.updateIsAdmin().subscribe({ error: () => {} });
    expect(service.isAdmin()).toBe(false);
    const received = vi.fn();
    service.updateIsAdmin().subscribe(received);
    expect(received).toHaveBeenCalledWith(false);
    expect(api.isAdmin).toHaveBeenCalledOnce();
  });
});
