import { signal } from '@angular/core';
import { AuthService } from '../../core/services/api-services/auth-service';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { MyPortalService } from './my-portal.service';

describe('Portal private value caches', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: {
            userData: signal({
              userId: 'user',
              fullName: 'Employee',
              hourlyRate: null,
              annualHolidayAllowance: null,
            }),
          },
        },
        provideHttpClient(),
        provideHttpClientTesting(),
        MyPortalService,
      ],
    }),
  );
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('checks profile and year caches before requesting data', () => {
    const service = TestBed.inject(MyPortalService);
    const http = TestBed.inject(HttpTestingController);
    service.loadDetails(2026);
    http.expectNone((request) => request.url.endsWith('/Users/Me'));
    http.expectOne((request) => request.url.endsWith('/DailyWorkRecords')).flush([]);
    service.loadDetails(2026);
    http.expectNone((request) => request.method === 'GET');
    expect(service.profile()?.fullName).toBe('Employee');

    service.loadDetails(2025);
    http.expectNone((request) => request.url.endsWith('/Users/Me'));
    http.expectOne((request) => request.url.endsWith('/DailyWorkRecords')).flush([]);
  });

  it('does not treat failed requests as cached values', () => {
    const service = TestBed.inject(MyPortalService);
    const http = TestBed.inject(HttpTestingController);
    service.loadPayments(2026, 1, 5);
    http
      .expectOne((request) => request.url.endsWith('/Payments'))
      .flush('Failed', { status: 500, statusText: 'Error' });
    service.loadPayments(2026, 1, 5);
    http
      .expectOne((request) => request.url.endsWith('/Payments'))
      .flush({
        items: [],
        summary: { totalCount: 0, approvedAmount: 0, pendingAmount: 0 },
        latestApproved: null,
      });
    service.loadPayments(2026, 1, 5);
    http.expectNone((request) => request.url.endsWith('/Payments'));
  });
});
