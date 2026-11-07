import { toTimeValue } from '../../../core/utils/attendance-time';
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

  it('loads today and saves nullable clock times before completing the same record', async () => {
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
          record: {
            id: 'existing-day',
            userId: 'existing',
            date,
            clockIn: null,
            clockOut: null,
            state: 1,
          },
        },
      ],
      totalCount: 2,
    });
    await fixture.whenStable();

    const rows = host.querySelectorAll('tbody tr');
    const clockIn = rows[0].querySelector('[formControlName="clockIn"]') as HTMLInputElement;
    const clockOut = rows[0].querySelector('[formControlName="clockOut"]') as HTMLInputElement;
    expect(clockIn.value).toBe('');
    expect(clockOut.value).toBe('');
    expect(clockOut.disabled).toBe(true);

    clockIn.value = '08:00';
    clockIn.dispatchEvent(new Event('input'));
    (rows[0].querySelector('button') as HTMLButtonElement).click();
    const create = http.expectOne((r) => r.method === 'POST');
    expect(create.request.body).toEqual({
      userId: 'employee',
      date,
      clockIn: toTimeValue('08:00'),
      clockOut: null,
      state: 0,
    });
    create.flush({ ...create.request.body, id: 'new-day' });
    await fixture.whenStable();

    clockOut.value = '15:30';
    clockOut.dispatchEvent(new Event('input'));
    (rows[0].querySelector('button') as HTMLButtonElement).click();
    const update = http.expectOne((r) => r.method === 'PUT');
    expect(update.request.url).toContain('new-day');
    expect(update.request.body).toEqual({
      clockIn: toTimeValue('08:00'),
      clockOut: toTimeValue('15:30'),
      state: 0,
    });
    update.flush({ ...update.request.body, userId: 'employee', date, id: 'new-day' });
    await fixture.whenStable();
    expect(clockOut.value).toBe('15:30');

    clockIn.value = '';
    clockIn.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    expect(clockOut.disabled).toBe(true);
    expect(clockOut.value).toBe('');
    http.expectNone(() => true);
  });

  it('defaults empty fields to the current time only when selected and unlocks clock-out after clock-in', async () => {
    const fixture = TestBed.createComponent(AttendanceComponent);
    const http = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
    http
      .expectOne((request) => request.url.includes('/GetDayRecords'))
      .flush({ items: [{ userId: 'one', fullName: 'Employee', record: null }], totalCount: 1 });
    await fixture.whenStable();
    const host = fixture.nativeElement as HTMLElement;
    const clockIn = host.querySelector('[formControlName="clockIn"]') as HTMLInputElement;
    const clockOut = host.querySelector('[formControlName="clockOut"]') as HTMLInputElement;
    expect(clockIn.value).toBe('');
    expect(clockOut.disabled).toBe(true);
    const now = new Date();
    const time = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
    clockIn.dispatchEvent(new FocusEvent('focus'));
    await fixture.whenStable();
    expect(clockIn.value).toBe(time);
    expect(clockOut.disabled).toBe(false);
    expect(clockOut.value).toBe('');
    clockOut.dispatchEvent(new FocusEvent('focus'));
    await fixture.whenStable();
    expect(clockOut.value).toBe(time);
    clockIn.value = '09:30';
    clockIn.dispatchEvent(new Event('input'));
    clockIn.dispatchEvent(new FocusEvent('focus'));
    expect(clockIn.value).toBe('09:30');
  });
});
