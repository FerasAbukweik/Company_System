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

  it('requests only the visible month and reuses cached months', () => {
    const service = TestBed.inject(MyPortalService);
    const http = TestBed.inject(HttpTestingController);
    service.loadDetails(new Date(2024, 1, 1));
    http.expectNone((request) => request.url.endsWith('/Users/Me'));
    const february = http.expectOne((request) => request.url.endsWith('/DailyWorkRecords'));
    expect(february.request.params.get('from')).toBe('2024-02-01');
    expect(february.request.params.get('to')).toBe('2024-02-29');
    february.flush([]);
    service.loadDetails(new Date(2024, 1, 1));
    http.expectNone((request) => request.method === 'GET');
    expect(service.profile()?.fullName).toBe('Employee');

    service.loadDetails(new Date(2024, 2, 1));
    const march = http.expectOne((request) => request.url.endsWith('/DailyWorkRecords'));
    expect(march.request.params.get('from')).toBe('2024-03-01');
    expect(march.request.params.get('to')).toBe('2024-03-31');
    march.flush([]);
    service.loadDetails(new Date(2024, 1, 1));
    http.expectNone((request) => request.method === 'GET');
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
        totalCount: 0,
      });
    service.loadPayments(2026, 1, 5);
    http.expectNone((request) => request.url.endsWith('/Payments'));
  });
  it('stores the unfiltered overview in a readonly signal and reuses it across month changes', () => {
    const service = TestBed.inject(MyPortalService);
    const http = TestBed.inject(HttpTestingController);
    service.loadOverview();
    const request = http.expectOne((request) => request.url.endsWith('/GetMyOverview'));
    expect(request.request.params.keys()).toEqual([]);
    expect(service.overview()).toBeNull();
    const overview = {
      approvedAmount: 1200,
      pendingAmount: 300,
      paidLeaveDays: 4,
      latestApproved: null,
    };
    request.flush(overview);
    expect(service.overview()).toEqual(overview);
    service.loadOverview();
    http.expectNone(() => true);
  });
});
