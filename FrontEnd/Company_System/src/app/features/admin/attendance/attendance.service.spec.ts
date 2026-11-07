import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AttendanceService } from './attendance.service';

const date = '2026-11-05';
const employee = { userId: 'employee', fullName: 'Employee', record: null };
const record = {
  id: 'day',
  userId: 'employee',
  date,
  state: 0,
  clockIn: '08:00:00',
  clockOut: '16:00:00',
};
const response = { items: [employee], totalCount: 6 };
const isRecords = (request: { url: string }) => request.url.includes('/GetDayRecords');

describe('Attendance combined page cache', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), AttendanceService],
    }),
  );
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('fetches names and attendance in one request and reuses pages and dates', () => {
    const service = TestBed.inject(AttendanceService);
    const http = TestBed.inject(HttpTestingController);
    service.loadDay(date, 1, 5);
    const first = http.expectOne(isRecords);
    expect(first.request.params.get('sectionSize')).toBe('5');
    expect(first.request.params.has('userIds')).toBe(false);
    expect(service.attendancePages()[1]).toBeUndefined();
    first.flush(response);
    expect(service.attendancePages()[1][0].fullName).toBe('Employee');

    service.loadDay(date, 2, 5);
    const second = http.expectOne(isRecords);
    expect(second.request.params.get('taken')).toBe('5');
    second.flush({ items: [{ ...employee, userId: 'second' }], totalCount: 6 });
    const pages = service.attendancePages();
    service.loadDay(date, 1, 5);
    http.expectNone(() => true);
    expect(service.attendancePages()).toBe(pages);

    service.loadDay('2026-11-06', 1, 5);
    http.expectOne(isRecords).flush(response);
    service.loadDay(date, 1, 5);
    http.expectNone(() => true);
    expect(service.attendancePages()[1][0]).toEqual(employee);
  });

  it('changes page size without calling the employees endpoint', () => {
    const service = TestBed.inject(AttendanceService);
    const http = TestBed.inject(HttpTestingController);
    service.loadDay(date, 1, 5);
    http.expectOne(isRecords).flush(response);
    service.loadDay(date, 1, 10);
    const resized = http.expectOne(isRecords);
    expect(resized.request.params.get('sectionSize')).toBe('10');
    resized.flush({ items: [], totalCount: 0 });
  });

  it('cancels an obsolete date request', () => {
    const service = TestBed.inject(AttendanceService);
    const http = TestBed.inject(HttpTestingController);
    service.loadDay(date, 1, 5);
    const obsolete = http.expectOne(isRecords);
    service.loadDay('2026-11-06', 1, 5);
    expect(obsolete.cancelled).toBe(true);
    http.expectOne(isRecords).flush(response);
    expect(service.loading()).toBe(false);
  });

  it('updates cached rows only after a successful save', () => {
    const service = TestBed.inject(AttendanceService);
    const http = TestBed.inject(HttpTestingController);
    service.loadDay(date, 1, 5);
    http.expectOne(isRecords).flush(response);
    service.save(record).subscribe({ error: () => {} });
    http
      .expectOne((request) => request.method === 'POST')
      .flush('Failed', { status: 500, statusText: 'Error' });
    expect(service.attendancePages()[1][0].record).toBeNull();
    service.save(record).subscribe();
    http.expectOne((request) => request.method === 'POST').flush(record);
    service.loadDay(date, 1, 5);
    http.expectNone(() => true);
    expect(service.attendancePages()[1][0].record).toEqual(record);
  });
});
