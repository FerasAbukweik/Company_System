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
  summary: { totalCount: 12, approvedAmount: 1200, pendingAmount: 300 },
  latestApproved: null,
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
    http.expectOne((request) => request.url.endsWith('/DailyWorkRecords')).flush([]);
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
});
