import { signal } from '@angular/core';
import { AuthService } from '../../core/services/api-services/auth-service';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { MyPortalComponent } from './my-portal.component';

const currentYear = new Date().getFullYear();
const result = (start: number, count: number) => ({
  items: Array.from({ length: count }, (_, i) => ({
    id: `payment-${start + i}`,
    userId: 'user',
    year: currentYear,
    month: 12 - i,
    paidAmount: 100,
    paymentDate: `${currentYear}-11-01`,
    state: 0,
  })),
  totalCount: 12,
});

describe('My Portal cached server pages', () => {
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
      ],
    }),
  );
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  const setup = async () => {
    const fixture = TestBed.createComponent(MyPortalComponent);
    const http = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
    http.expectNone((request) => request.url.endsWith('/Users/Me'));
    const attendance = http.expectOne((request) => request.url.endsWith('/DailyWorkRecords'));
    const now = new Date();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    expect(attendance.request.params.get('from')).toBe(`${currentYear}-${month}-01`);
    expect(attendance.request.params.get('to')).toBe(
      `${currentYear}-${month}-${new Date(currentYear, now.getMonth() + 1, 0).getDate()}`,
    );
    attendance.flush([]);
    const overview = http.expectOne((request) => request.url.endsWith('/GetMyOverview'));
    expect(overview.request.params.keys()).toEqual([]);
    overview.flush({
      approvedAmount: 1200,
      pendingAmount: 300,
      paidLeaveDays: 4,
      latestApproved: null,
    });
    const first = http.expectOne((request) => request.url.endsWith('/Payments'));
    expect(first.request.params.get('sectionSize')).toBe('5');
    expect(first.request.params.get('taken')).toBe('0');
    // Deliberately return more than the requested size: the table must render every returned record.
    first.flush(result(0, 7));
    await fixture.whenStable();
    return { fixture, http, element: fixture.nativeElement as HTMLElement };
  };

  it('lists every year from 2023 through the current year', async () => {
    const { element } = await setup();
    const options = Array.from(element.querySelectorAll('[aria-label="Portal year"] option')).map(
      (option) => Number(option.textContent),
    );
    expect((element.querySelector('[aria-label="Portal year"]') as HTMLSelectElement).value).toBe(
      String(currentYear),
    );
    expect(options).toEqual(Array.from({ length: currentYear - 2023 + 1 }, (_, i) => 2023 + i));
  });

  it('shows all backend rows and reuses previously fetched pages without another request', async () => {
    const { fixture, http, element } = await setup();
    expect(element.querySelectorAll('tbody tr')).toHaveLength(7);
    (element.querySelector('[aria-label="Next page"]') as HTMLButtonElement).click();
    await fixture.whenStable();
    const next = http.expectOne((request) => request.url.endsWith('/Payments'));
    expect(next.request.params.get('taken')).toBe('5');
    expect(next.request.params.get('sectionSize')).toBe('5');
    next.flush(result(5, 5));
    await fixture.whenStable();
    (element.querySelector('[aria-label="Previous page"]') as HTMLButtonElement).click();
    await fixture.whenStable();
    http.expectNone((request) => request.url.endsWith('/Payments'));
    expect(element.querySelectorAll('tbody tr')).toHaveLength(7);
    expect(element.querySelector('app-portal-summary')?.textContent).toContain('1,200.00');
  });

  it('uses the selected table size and clears cached pages when it changes', async () => {
    const { fixture, http, element } = await setup();
    const size = element.querySelector('app-employee-pagination select') as HTMLSelectElement;
    size.value = '10';
    size.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    const resized = http.expectOne((request) => request.url.endsWith('/Payments'));
    expect(resized.request.params.get('sectionSize')).toBe('10');
    expect(resized.request.params.get('taken')).toBe('0');
    resized.flush(result(0, 10));
    await fixture.whenStable();
    expect(element.querySelectorAll('tbody tr')).toHaveLength(10);
  });
  it('loads the visible month, shows selected clock times and reuses the previous month', async () => {
    const { fixture, http, element } = await setup();
    const now = new Date();
    const move = now.getMonth() === 0 ? 'Next month' : 'Previous month';
    const back = now.getMonth() === 0 ? 'Previous month' : 'Next month';
    (element.querySelector(`[aria-label="${move}"]`) as HTMLButtonElement).click();
    await fixture.whenStable();
    const attendance = http.expectOne((request) => request.url.endsWith('/DailyWorkRecords'));
    const month = new Date(currentYear, now.getMonth() + (now.getMonth() === 0 ? 1 : -1), 1);
    const date = `${currentYear}-${String(month.getMonth() + 1).padStart(2, '0')}-01`;
    expect(attendance.request.params.get('from')).toBe(date);
    attendance.flush([
      { id: 'record', userId: 'user', date, clockIn: '08:15:00', clockOut: '16:45:00', state: 0 },
    ]);
    await fixture.whenStable();
    const detail = element.querySelector('[aria-label="Selected day attendance"]')!;
    expect(detail.textContent).toContain('08:15');
    expect(detail.textContent).toContain('16:45');
    expect(detail.textContent).toContain('8.5 hours worked');
    const nextDate = date.slice(0, -2) + '02';
    (element.querySelector(`[aria-label^="${nextDate} ·"]`) as HTMLButtonElement).click();
    await fixture.whenStable();
    expect(detail.textContent).toContain('No attendance record');
    expect(detail.textContent).toContain('Not recorded');
    http.expectNone(() => true);
    (element.querySelector(`[aria-label="${back}"]`) as HTMLButtonElement).click();
    await fixture.whenStable();
    http.expectNone(() => true);
  });
  it('does not refetch the overview when the year filter changes', async () => {
    const { fixture, http, element } = await setup();
    const year = element.querySelector('[aria-label="Portal year"]') as HTMLSelectElement;
    year.value = String(currentYear - 1);
    year.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    http.expectOne((request) => request.url.endsWith('/DailyWorkRecords')).flush([]);
    http
      .expectOne((request) => request.url.endsWith('/Payments'))
      .flush({ items: [], totalCount: 0 });
    http.expectNone((request) => request.url.endsWith('/GetMyOverview'));
    await fixture.whenStable();
    expect(element.querySelector('app-portal-summary')?.textContent).toContain('1,200.00');
  });
  it('filters by every payment state, resets pagination and restores cached all-payment data', async () => {
    const { fixture, http, element } = await setup();
    const status = element.querySelector('[aria-label="Payment status"]') as HTMLSelectElement;
    expect(status.value).toBe('');
    (element.querySelector('[aria-label="Next page"]') as HTMLButtonElement).click();
    await fixture.whenStable();
    http.expectOne((request) => request.url.endsWith('/Payments')).flush(result(5, 5));
    await fixture.whenStable();

    for (const state of [0, 1, 2]) {
      status.value = String(state);
      status.dispatchEvent(new Event('change'));
      await fixture.whenStable();
      const request = http.expectOne((request) => request.url.endsWith('/Payments'));
      expect(request.request.params.get('state')).toBe(String(state));
      expect(request.request.params.get('taken')).toBe('0');
      request.flush({ items: [], totalCount: 0 });
      await fixture.whenStable();
      http.expectNone(() => true);
    }

    status.value = '';
    status.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    http.expectNone(() => true);
    expect(element.querySelectorAll('tbody tr')).toHaveLength(7);
    expect(element.querySelector('app-portal-summary')?.textContent).toContain('1,200.00');
  });
});
