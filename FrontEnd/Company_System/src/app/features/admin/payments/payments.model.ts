import { AdminPayment } from '../../../core/dto/payments/admin-payment-dto';
import { PaymentState } from '../../../core/enum/payment-state';

export type PaymentView = 'pending' | 'history' | 'all';
export type HistoryFilter = 'all' | 'approved' | 'rejected';

export interface PaymentDecision {
  payments: readonly AdminPayment[];
  state: PaymentState.Approved | PaymentState.Rejected;
}
