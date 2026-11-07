import { Router } from '@angular/router';
import { AuthService } from './auth-service';
import { ToastService } from './toast-service';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { UsersService } from './users-service';
import { PaymentsService } from './payments-service';
import { DailyWorkRecordsService } from './daily-work-records-service';
import { PaymentState } from '../../enum/payment-state';

const query = { taken: 0, sectionSize: 5 };
const page = { items: [{ id: 'employee' }], totalCount: 1, summary: {} };

describe('Shared controller caches', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }),
  );
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('caches employee pages and profile only after a response and separates filters', () => {
    const users = TestBed.inject(UsersService);
    const http = TestBed.inject(HttpTestingController);
    users.employeePage(query).subscribe();
    http.expectOne(() => true).flush(page);
    const received = vi.fn();
    users.employeePage(query).subscribe(received);
    http.expectNone(() => true);
    expect(received).toHaveBeenCalledWith(page);
    users.employeePage({ ...query, search: 'Alice' }).subscribe();
    http.expectOne(() => true).flush({ ...page, items: [] });
    users.me().subscribe();
    http.expectOne(() => true).flush({ id: 'employee' });
    users.me().subscribe();
    http.expectNone(() => true);
  });

  it('caches payment pages including empty responses, using state and pagination as keys', () => {
    const payments = TestBed.inject(PaymentsService);
    const http = TestBed.inject(HttpTestingController);
    payments.payments({ ...query, year: 2026 }).subscribe();
    http.expectOne(() => true).flush(page);
    payments.payments({ ...query, year: 2026 }).subscribe();
    http.expectNone(() => true);
    payments.payments({ ...query, year: 2026, state: PaymentState.Pending }).subscribe();
    http.expectOne(() => true).flush({ ...page, items: [] });
    payments.payments({ ...query, year: 2026, state: PaymentState.Pending }).subscribe();
    http.expectNone(() => true);
    payments.myPayments(2026).subscribe();
    http.expectOne(() => true).flush({ ...page, latestApproved: null });
    payments.myPayments(2026).subscribe();
    http.expectNone(() => true);
  });

  it('keeps payment caches after a rejected write and invalidates both caches after success', () => {
    const payments = TestBed.inject(PaymentsService);
    const http = TestBed.inject(HttpTestingController);
    payments.payments({ ...query, year: 2026 }).subscribe();
    http.expectOne(() => true).flush(page);
    payments.myPayments(2026).subscribe();
    http.expectOne(() => true).flush({ ...page, latestApproved: null });
    payments.decide(['payment'], PaymentState.Approved).subscribe({ error: () => {} });
    http.expectOne(() => true).flush(null, { status: 500, statusText: 'Error' });
    payments.payments({ ...query, year: 2026 }).subscribe();
    http.expectNone(() => true);
    payments.decide(['payment'], PaymentState.Approved).subscribe();
    http.expectOne(() => true).flush([]);
    payments.payments({ ...query, year: 2026 }).subscribe();
    http.expectOne(() => true).flush(page);
    payments.myPayments(2026).subscribe();
    http.expectOne(() => true).flush({ ...page, latestApproved: null });
  });

  it('reuses attendance across callers and invalidates day and period data after saving', () => {
    const attendance = TestBed.inject(DailyWorkRecordsService);
    const http = TestBed.inject(HttpTestingController);
    attendance.attendanceDayRecords('2026-11-06', query).subscribe();
    http.expectOne(() => true).flush({ items: [], totalCount: 0 });
    attendance.attendanceDayRecords('2026-11-06', query).subscribe();
    http.expectNone(() => true);
    attendance.attendance('2026-01-01', '2026-12-31').subscribe();
    http.expectOne(() => true).flush({ items: [], totalCount: 0 });
    attendance.attendance('2026-01-01', '2026-12-31').subscribe();
    http.expectNone(() => true);
    attendance
      .saveAttendance({ userId: 'a', date: '2026-11-06', clockIn: '08:00:00', clockOut: '16:00:00', state: 0 })
      .subscribe();
    http.expectOne(() => true).flush({ id: 'record' });
    attendance.attendanceDayRecords('2026-11-06', query).subscribe();
    http.expectOne(() => true).flush({ items: [], totalCount: 0 });
    attendance.attendance('2026-01-01', '2026-12-31').subscribe();
    http.expectOne(() => true).flush([]);
  });

  it('does not retain failed or obsolete reads in the shared cache', () => {
    const users = TestBed.inject(UsersService);
    const http = TestBed.inject(HttpTestingController);
    users.employeePage(query).subscribe({ error: () => {} });
    http.expectOne(() => true).flush(null, { status: 500, statusText: 'Error' });
    users.employeePage(query).subscribe();
    const obsolete = http.expectOne(() => true);
    users.reset();
    obsolete.flush(page);
    users.employeePage(query).subscribe();
    http.expectOne(() => true).flush(page);
  });
  it('clears shared data on session end so another user cannot reuse it', () => {
    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: {} },
        { provide: ToastService, useValue: {} },
      ],
    });
    const users = TestBed.inject(UsersService);
    const payments = TestBed.inject(PaymentsService);
    const attendance = TestBed.inject(DailyWorkRecordsService);
    const http = TestBed.inject(HttpTestingController);
    users.me().subscribe();
    http.expectOne(() => true).flush({ id: 'previous-user' });
    payments.myPayments(2026).subscribe();
    http.expectOne(() => true).flush({ items: [], summary: {}, latestApproved: null });
    attendance.attendance('2026-01-01', '2026-12-31').subscribe();
    http.expectOne(() => true).flush([]);

    TestBed.inject(AuthService).clearSession();

    users.me().subscribe();
    http.expectOne(() => true).flush({ id: 'next-user' });
    payments.myPayments(2026).subscribe();
    http.expectOne(() => true).flush({ items: [], summary: {}, latestApproved: null });
    attendance.attendance('2026-01-01', '2026-12-31').subscribe();
    http.expectOne(() => true).flush([]);
  });
});
