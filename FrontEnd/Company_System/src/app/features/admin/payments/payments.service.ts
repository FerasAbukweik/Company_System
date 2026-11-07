import { apiError } from '../../../core/utils/api-error';
import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { defer, finalize, of, tap, Subscription } from 'rxjs';
import { PaymentsService as PaymentsDataService } from '../../../core/services/api-services/payments-service';
import { AdminPayment } from '../../../core/dto/payments/admin-payment-dto';
import { AdminPaymentQuery } from '../../../core/dto/payments/admin-payment-query-dto';
import { PaymentPage } from '../../../core/dto/payments/payment-page-dto';
import { PaymentDecision } from './payments.model';

@Injectable()
export class PaymentsService {
  private readonly paymentDataService = inject(PaymentsDataService);
  private readonly destroyRef = inject(DestroyRef);
  private request?: Subscription;
  private query?: AdminPaymentQuery;
  private cacheScope: string | null = null;

  readonly paymentPages = signal<Readonly<Record<number, AdminPayment[]>>>({});
  readonly totalCount = signal(0);
  readonly summary = this.paymentDataService.summary;
  readonly summaryError = signal('');
  private summaryRequest?: Subscription;
  readonly loading = signal(false);
  readonly error = signal('');
  readonly years = signal(
    Array.from({ length: new Date().getFullYear() - 2023 + 1 }, (_, i) => 2023 + i),
  );

  constructor() {
    this.loadSummary();
    this.destroyRef.onDestroy(() => {
      this.request?.unsubscribe();
      this.summaryRequest?.unsubscribe();
    });
  }

  loadSummary() {
    this.summaryRequest?.unsubscribe();
    this.summaryError.set('');
    this.summaryRequest = this.paymentDataService.getSummary().subscribe({
      error: (error) => this.summaryError.set(apiError(error)),
    });
  }

  private fetchPage(query: AdminPaymentQuery) {
    return defer(() => {
      const scope = JSON.stringify([
        query.year,
        query.month ?? null,
        query.state ?? null,
        query.historyOnly ?? false,
        query.fullName?.trim() ?? '',
        query.sectionSize,
      ]);
      if (scope !== this.cacheScope) {
        this.cacheScope = scope;
        this.paymentPages.set({});
      }
      const page = Math.floor(query.taken / query.sectionSize) + 1;
      const cached = this.paymentPages()[page];
      if (cached !== undefined) return of(cached);

      return this.paymentDataService.payments(query).pipe(
        tap((result) => {
          if (scope !== this.cacheScope) return;
          this.publishPage(page, result);
        }),
      );
    });
  }

  private publishPage(page: number, result: PaymentPage) {
    this.paymentPages.update((pages) => ({ ...pages, [page]: result.items }));
    this.totalCount.set(result.totalCount);
  }

  load(query: AdminPaymentQuery) {
    this.request?.unsubscribe();
    this.query = query;
    this.loading.set(true);
    this.error.set('');
    this.request = this.fetchPage(query)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        error: (error) => this.error.set(apiError(error)),
      });
  }

  reload() {
    this.paymentDataService.reset();
    this.loadSummary();
    this.paymentPages.set({});
    if (this.query) this.load(this.query);
  }

  applyDecision(decision: PaymentDecision) {
    return this.paymentDataService
      .decide(
        decision.payments.map((payment) => payment.id),
        decision.state,
      )
      .pipe(tap(() => this.reload()));
  }
}
