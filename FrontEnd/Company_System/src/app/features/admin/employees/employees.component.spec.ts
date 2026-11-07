import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { EmployeesComponent } from './employees.component';
import { EmployeeDrawerComponent } from './components/employee-drawer/employee-drawer.component';

const summary = { headcount: 12, active: 10, totalHourlyRates: 120000, configuredHourlyRates: 10 };
const page = (start: number, count: number) => ({
  items: Array.from({ length: count }, (_, i) => ({
    id: `user-${start + i}`,
    fullName: `Employee ${start + i}`,
    userName: `employee${start + i}`,
    email: `employee${start + i}@example.com`,
    phone: '',
    position: 4,
    hourlyRate: 12000,
    imageUrl: null,
    annualHolidayAllowance: null,
    isActive: true,
  })),
  totalCount: 12,
});

describe('Employee server pagination', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).overrideComponent(EmployeeDrawerComponent, { set: { template: '' } }),
  );
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('fetches five employees initially and only fetches the next page after navigation', async () => {
    const fixture = TestBed.createComponent(EmployeesComponent);
    const http = TestBed.inject(HttpTestingController);
    const aggregate = http.expectOne((request) => request.url.endsWith('/GetEmployeesSummary'));
    expect(aggregate.request.params.keys()).toEqual([]);
    aggregate.flush(summary);
    await fixture.whenStable();
    const first = http.expectOne((request) => request.url.endsWith('/Users/GetEmployees'));
    expect(first.request.params.get('taken')).toBe('0');
    expect(first.request.params.get('sectionSize')).toBe('5');
    expect(fixture.nativeElement.querySelectorAll('app-employee-toolbar input')).toHaveLength(1);
    expect(fixture.nativeElement.querySelector('[aria-label="Filter by position"]')).toBeNull();
    first.flush(page(0, 7));
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(7);
    http.expectNone((request) => request.url.endsWith('/Users/GetEmployees'));

    fixture.nativeElement.querySelector('[aria-label="Next page"]').click();
    await fixture.whenStable();
    const next = http.expectOne((request) => request.url.endsWith('/Users/GetEmployees'));
    expect(next.request.params.get('taken')).toBe('5');
    expect(next.request.params.get('sectionSize')).toBe('5');
    next.flush(page(5, 5));
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('tbody').textContent).toContain('Employee 5');
    expect(fixture.nativeElement.querySelector('tbody').textContent).not.toContain('Employee 0');
    fixture.nativeElement.querySelector('[aria-label="Previous page"]').click();
    await fixture.whenStable();
    http.expectNone((request) => request.url.endsWith('/Users/GetEmployees'));
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(7);
  });

  it('sends the selected page size and filters to the server, resetting to page one', async () => {
    const fixture = TestBed.createComponent(EmployeesComponent);
    const http = TestBed.inject(HttpTestingController);
    const aggregate = http.expectOne((request) => request.url.endsWith('/GetEmployeesSummary'));
    expect(aggregate.request.params.keys()).toEqual([]);
    aggregate.flush(summary);
    await fixture.whenStable();
    http.expectOne((request) => request.url.endsWith('/Users/GetEmployees')).flush(page(0, 5));
    await fixture.whenStable();
    const size = fixture.nativeElement.querySelector(
      'app-employee-pagination select',
    ) as HTMLSelectElement;
    size.value = '10';
    size.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    const resized = http.expectOne((request) => request.url.endsWith('/Users/GetEmployees'));
    expect(resized.request.params.get('sectionSize')).toBe('10');
    expect(resized.request.params.get('taken')).toBe('0');
    resized.flush(page(0, 10));
    await fixture.whenStable();

    const search = fixture.nativeElement.querySelector(
      '[aria-label="Search employees"]',
    ) as HTMLInputElement;
    search.value = 'Employee 11';
    search.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    http.expectNone((request) => request.url.endsWith('/Users/GetEmployees'));
    await new Promise((resolve) => setTimeout(resolve, 350));
    await fixture.whenStable();
    const filtered = http.expectOne((request) => request.url.endsWith('/Users/GetEmployees'));
    expect(filtered.request.params.get('search')).toBe('Employee 11');
    expect(filtered.request.params.get('taken')).toBe('0');
    filtered.flush({ ...page(11, 1), totalCount: 1 });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(1);
  });
});
