import { inject, Injectable, signal } from '@angular/core';
import { defer, of, tap } from 'rxjs';
import { PaymentsApiService } from '../api/payments-api-service';
import { PaymentSummary } from '../../dto/payments/payment-summary-dto';
import { PaymentPage } from '../../dto/payments/payment-page-dto';
import { UserPaymentOverview } from '../../dto/payments/user-payment-overview-dto';
import { UserPaymentPage } from '../../dto/payments/user-payment-page-dto';
import { AdminPaymentQuery } from '../../dto/payments/admin-payment-query-dto';
import { PaymentState } from '../../enum/payment-state';

@Injectable({ providedIn: 'root' })
export class PaymentsService {
  private readonly api = inject(PaymentsApiService);
  private paymentPages: Record<string, PaymentPage> | null = null;
  private userPaymentPages: Record<string, UserPaymentPage> | null = null;
  private version = 0;
  private readonly _summary = signal<PaymentSummary | null>(null);
  public readonly summary = this._summary.asReadonly();
  private readonly _overview = signal<UserPaymentOverview | null>(null);
  public readonly overview = this._overview.asReadonly();

  getSummary() {
    return defer(() => {
      const cached = this._summary();
      if (cached !== null) return of(cached);
      const version = this.version;
      return this.api.summary().pipe(
        tap((summary) => {
          if (version === this.version) this._summary.set(summary);
        }),
      );
    });
  }

  payments(query: AdminPaymentQuery) {
    return defer(() => {
      const key = JSON.stringify([
        query.year,
        query.month ?? null,
        query.state ?? null,
        query.historyOnly ?? false,
        query.fullName?.trim() ?? '',
        query.taken,
        query.sectionSize,
      ]);
      const cached = this.paymentPages?.[key];
      if (cached !== undefined) return of(cached);
      const version = this.version;

      return this.api.payments(query).pipe(
        tap((page) => {
          if (version !== this.version) return;
          this.paymentPages ??= {};
          this.paymentPages[key] = page;
        }),
      );
    });
  }

  myPayments(year: number, state: PaymentState | null = null, taken = 0, sectionSize = 5) {
    return defer(() => {
      const key = JSON.stringify([year, state, taken, sectionSize]);
      const cached = this.userPaymentPages?.[key];
      if (cached !== undefined) return of(cached);
      const version = this.version;

      return this.api.myPayments(year, state, taken, sectionSize).pipe(
        tap((page) => {
          if (version !== this.version) return;
          this.userPaymentPages ??= {};
          this.userPaymentPages[key] = page;
        }),
      );
    });
  }

  myOverview() {
    return defer(() => {
      const cached = this._overview();
      if (cached !== null) return of(cached);
      const version = this.version;

      return this.api.myOverview().pipe(
        tap((overview) => {
          if (version === this.version) this._overview.set(overview);
        }),
      );
    });
  }

  resetOverview() {
    this.version++;
    this._overview.set(null);
  }

  decide(ids: string[], state: PaymentState) {
    return this.api.decide(ids, state).pipe(tap(() => this.reset()));
  }

  reset() {
    this.paymentPages = null;
    this._summary.set(null);
    this.userPaymentPages = null;
    this.resetOverview();
  }
}
