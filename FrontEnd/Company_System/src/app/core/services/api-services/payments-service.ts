import { inject, Injectable } from '@angular/core';
import { defer, of, tap } from 'rxjs';
import { PaymentsApiService } from '../api/payments-api-service';
import { PaymentPage } from '../../dto/payments/payment-page-dto';
import { UserPaymentPage } from '../../dto/payments/user-payment-page-dto';
import { AdminPaymentQuery } from '../../dto/payments/admin-payment-query-dto';
import { PaymentState } from '../../enum/payment-state';

@Injectable({ providedIn: 'root' })
export class PaymentsService {
  private readonly api = inject(PaymentsApiService);
  private paymentPages: Record<string, PaymentPage> | null = null;
  private userPaymentPages: Record<string, UserPaymentPage> | null = null;
  private version = 0;

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

  decide(ids: string[], state: PaymentState) {
    return this.api.decide(ids, state).pipe(tap(() => this.reset()));
  }

  reset() {
    this.version++;
    this.paymentPages = null;
    this.userPaymentPages = null;
  }
}
