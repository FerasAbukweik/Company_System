import { apiError } from '../../core/utils/api-error';
import { UserPaymentPage } from '../../core/dto/payments/user-payment-page-dto';
import { AttendanceRecord } from '../../core/dto/attendance/attendance-record-dto';
import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { defer, finalize, forkJoin, of, tap, Subscription } from 'rxjs';
import { AuthService } from '../../core/services/api-services/auth-service';
import { DailyWorkRecordsService } from '../../core/services/api-services/daily-work-records-service';
import { PaymentsService as PaymentsDataService } from '../../core/services/api-services/payments-service';
import { AdminPayment } from '../../core/dto/payments/admin-payment-dto';
import { UserDTO } from '../../core/dto/auth/user-dto';

@Injectable()
export class MyPortalService {
  private readonly authService = inject(AuthService);
  private readonly workRecordsService = inject(DailyWorkRecordsService);
  private readonly paymentsService = inject(PaymentsDataService);
  private readonly destroyRef = inject(DestroyRef);
  private detailsRequest?: Subscription;
  private paymentRequest?: Subscription;
  private cacheScope: string | null = null;
  private attendanceCache: Record<number, AttendanceRecord[]> | null = null;

  readonly profile = signal<UserDTO | null>(null);
  readonly attendance = signal<AttendanceRecord[]>([]);
  readonly portalPaymentPages = signal<Readonly<Record<number, AdminPayment[]>>>({});
  readonly paymentSummary = signal({ totalCount: 0, approvedAmount: 0, pendingAmount: 0 });
  readonly latestApproved = signal<AdminPayment | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly paymentsLoading = signal(false);
  readonly paymentsError = signal('');

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.detailsRequest?.unsubscribe();
      this.paymentRequest?.unsubscribe();
    });
  }

  loadDetails(year: number) {
    this.detailsRequest?.unsubscribe();
    this.loading.set(true);
    this.error.set('');
    this.profile.set(null);
    this.attendance.set([]);
    this.detailsRequest = forkJoin({
      profile: this.getProfile(),
      attendance: this.getAttendance(year),
    })
      .pipe(
        tap((result) => {
          this.profile.set(result.profile);
          this.attendance.set(result.attendance);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({ error: (error) => this.error.set(apiError(error)) });
  }

  private getProfile() {
    return of(this.authService.userData());
  }

  private getAttendance(year: number) {
    return defer(() => {
      const cached = this.attendanceCache?.[year];
      if (cached !== undefined) return of(cached);
      return this.workRecordsService.attendance(`${year}-01-01`, `${year}-12-31`).pipe(
        tap((records) => {
          this.attendanceCache ??= {};
          this.attendanceCache[year] = records;
        }),
      );
    });
  }

  private fetchPage(year: number, page: number, size: number) {
    return defer(() => {
      const scope = `${year}:${size}`;
      if (scope !== this.cacheScope) {
        this.cacheScope = scope;
        this.portalPaymentPages.set({});
      }
      const cached = this.portalPaymentPages()[page];
      if (cached?.length) return of(cached);

      return this.paymentsService.myPayments(year, null, (page - 1) * size, size).pipe(
        tap((result) => {
          if (scope !== this.cacheScope) return;
          this.publishPage(page, result);
        }),
      );
    });
  }

  private publishPage(page: number, result: UserPaymentPage) {
    this.portalPaymentPages.update((pages) => ({ ...pages, [page]: result.items }));
    this.paymentSummary.set(result.summary);
    this.latestApproved.set(result.latestApproved);
  }

  loadPayments(year: number, page: number, size: number) {
    this.paymentRequest?.unsubscribe();
    this.paymentsLoading.set(true);
    this.paymentsError.set('');
    this.paymentRequest = this.fetchPage(year, page, size)
      .pipe(finalize(() => this.paymentsLoading.set(false)))
      .subscribe({ error: (error) => this.paymentsError.set(apiError(error)) });
  }
}
