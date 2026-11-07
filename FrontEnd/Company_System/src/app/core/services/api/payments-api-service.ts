import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Urls } from '../../constants/urls';
import { UserPaymentOverview } from '../../dto/payments/user-payment-overview-dto';
import { UserPaymentPage } from '../../dto/payments/user-payment-page-dto';
import { AdminPayment } from '../../dto/payments/admin-payment-dto';
import { PaymentSummary } from '../../dto/payments/payment-summary-dto';
import { PaymentPage } from '../../dto/payments/payment-page-dto';
import { AdminPaymentQuery } from '../../dto/payments/admin-payment-query-dto';
import { PaymentState } from '../../enum/payment-state';

@Injectable({ providedIn: 'root' })
export class PaymentsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = Urls.api + '/Payments';

  summary() {
    return this.http.get<PaymentSummary>(this.base + '/GetSummary');
  }

  payments(query: AdminPaymentQuery) {
    return this.http.get<PaymentPage>(this.base + '/GetPayments', {
      params: {
        year: query.year,
        taken: query.taken,
        sectionSize: query.sectionSize,
        ...(query.month ? { month: query.month } : {}),
        ...(query.state == null ? {} : { state: query.state }),
        ...(query.historyOnly ? { historyOnly: true } : {}),
        ...(query.fullName?.trim() ? { fullName: query.fullName.trim() } : {}),
      },
    });
  }

  myPayments(year: number, state: PaymentState | null = null, taken = 0, sectionSize = 5) {
    return this.http.get<UserPaymentPage>(this.base, {
      params: { year, taken, sectionSize, ...(state === null ? {} : { state }) },
    });
  }

  myOverview() {
    return this.http.get<UserPaymentOverview>(this.base + '/GetMyOverview');
  }

  decide(ids: string[], state: PaymentState) {
    return this.http.put<AdminPayment[]>(this.base + '/UpdateStates', ids, {
      params: { state },
    });
  }
}
