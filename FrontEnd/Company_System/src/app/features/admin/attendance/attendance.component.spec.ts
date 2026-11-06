import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AttendanceComponent } from './attendance.component';

describe('Daily employee attendance table', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }),
  );
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('loads today automatically, defaults to eight hours, and saves and updates minutes', async () => {
    const fixture = TestBed.createComponent(AttendanceComponent);
    const http = TestBed.inject(HttpTestingController);
    const host = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
    expect(host.querySelector('app-lazy-select')).toBeNull();
    const today = host.querySelector('[aria-pressed="true"]')!;
    const date = today.getAttribute('aria-label')!.split(' · ')[0];
    http.expectNone((request) => request.url.includes('/Users/GetEmployees'));
    const query = http.expectOne((request) => request.url.includes('/GetDayRecords'));
    expect(query.request.params.get('date')).toBe(date);
    expect(query.request.params.get('sectionSize')).toBe('5');
    query.flush({
      items: [
        { userId: 'employee', fullName: 'New Employee', record: null },
        {
          userId: 'existing',
          fullName: 'Existing Employee',
          record: { id: 'existing-day', userId: 'existing', date, minutesWorked: 0, state: 1 },
        },
      ],
      totalCount: 2,
    });
    await fixture.whenStable();

    const rows = host.querySelectorAll('tbody tr');
    expect((rows[0].querySelector('input') as HTMLInputElement).value).toBe('8');
    expect((rows[1].querySelector('input') as HTMLInputElement).value).toBe('0');
    (rows[0].querySelector('button') as HTMLButtonElement).click();
    const create = http.expectOne((r) => r.method === 'POST');
    expect(create.request.body).toEqual({ userId: 'employee', date, minutesWorked: 480, state: 0 });
    create.flush({ ...create.request.body, id: 'new-day' });
    await fixture.whenStable();

    const hours = rows[0].querySelector('input') as HTMLInputElement;
    hours.value = '7.5';
    hours.dispatchEvent(new Event('input'));
    (rows[0].querySelector('button') as HTMLButtonElement).click();
    const update = http.expectOne((r) => r.method === 'PUT');
    expect(update.request.url).toContain('new-day');
    expect(update.request.body).toEqual({ minutesWorked: 450, state: 0 });
    update.flush({ userId: 'employee', date, id: 'new-day', minutesWorked: 450, state: 0 });
    await fixture.whenStable();
    expect(hours.value).toBe('7.5');

    hours.value = '25';
    hours.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    expect((rows[0].querySelector('button') as HTMLButtonElement).disabled).toBe(true);
    http.expectNone(() => true);
  });
});
