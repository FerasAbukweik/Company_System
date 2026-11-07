import { apiError } from '../../core/utils/api-error';
import { UserPaymentPage } from '../../core/dto/payments/user-payment-page-dto';
import { AttendanceRecord } from '../../core/dto/attendance/attendance-record-dto';
import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { defer, finalize, of, tap, Subscription } from 'rxjs';
import { AuthService } from '../../core/services/api-services/auth-service';
import { DailyWorkRecordsService } from '../../core/services/api-services/daily-work-records-service';
import { PaymentsService as PaymentsDataService } from '../../core/services/api-services/payments-service';
import { AdminPayment } from '../../core/dto/payments/admin-payment-dto';
import { PaymentState } from '../../core/enum/payment-state';
import { UserDTO } from '../../core/dto/auth/user-dto';

@Injectable()
export class MyPortalService {
  private readonly authService = inject(AuthService);
  private readonly workRecordsService = inject(DailyWorkRecordsService);
  private readonly paymentsService = inject(PaymentsDataService);
  private readonly destroyRef = inject(DestroyRef);
  private detailsRequest?: Subscription;
  private paymentRequest?: Subscription;
  private overviewRequest?: Subscription;
  private cacheScope: string | null = null;

  readonly profile = signal<UserDTO | null>(null);
  readonly attendance = signal<AttendanceRecord[]>([]);
  readonly overview = this.paymentsService.overview;
  readonly overviewLoading = signal(false);
  readonly overviewError = signal('');
  readonly portalPaymentPages = signal<Readonly<Record<number, AdminPayment[]>>>({});
  readonly paymentTotalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly paymentsLoading = signal(false);
  readonly paymentsError = signal('');

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.detailsRequest?.unsubscribe();
      this.paymentRequest?.unsubscribe();
      this.overviewRequest?.unsubscribe();
    });
  }

  loadDetails(month: Date) {
    const year = month.getFullYear();
    const monthNumber = String(month.getMonth() + 1).padStart(2, '0');
    const lastDay = new Date(year, month.getMonth() + 1, 0).getDate();
    this.detailsRequest?.unsubscribe();
    this.loading.set(true);
    this.error.set('');
    this.profile.set(this.authService.userData());
    this.attendance.set([]);
    this.detailsRequest = this.workRecordsService
      .attendance(`${year}-${monthNumber}-01`, `${year}-${monthNumber}-${lastDay}`)
      .pipe(
        tap((records) => this.attendance.set(records)),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({ error: (error) => this.error.set(apiError(error)) });
  }

  loadOverview() {
    this.overviewRequest?.unsubscribe();
    this.overviewLoading.set(true);
    this.overviewError.set('');
    this.overviewRequest = this.paymentsService
      .myOverview()
      .pipe(finalize(() => this.overviewLoading.set(false)))
      .subscribe({ error: (error) => this.overviewError.set(apiError(error)) });
  }

  private fetchPage(year: number, page: number, size: number, state: PaymentState | null) {
    return defer(() => {
      const scope = `${year}:${state ?? 'all'}:${size}`;
      if (scope !== this.cacheScope) {
        this.cacheScope = scope;
        this.portalPaymentPages.set({});
      }
      const cached = this.portalPaymentPages()[page];
      if (cached !== undefined) return of(cached);

      return this.paymentsService.myPayments(year, state, (page - 1) * size, size).pipe(
        tap((result) => {
          if (scope !== this.cacheScope) return;
          this.publishPage(page, result);
        }),
      );
    });
  }

  private publishPage(page: number, result: UserPaymentPage) {
    this.portalPaymentPages.update((pages) => ({ ...pages, [page]: result.items }));
    this.paymentTotalCount.set(result.totalCount);
  }

  loadPayments(year: number, page: number, size: number, state: PaymentState | null = null) {
    this.paymentRequest?.unsubscribe();
    this.paymentsLoading.set(true);
    this.paymentsError.set('');
    this.paymentRequest = this.fetchPage(year, page, size, state)
      .pipe(finalize(() => this.paymentsLoading.set(false)))
      .subscribe({ error: (error) => this.paymentsError.set(apiError(error)) });
  }
}
