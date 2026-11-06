import { AdminPayment } from './admin-payment-dto';
import { PaymentSummary } from './payment-summary-dto';

export interface PaymentPage {
  items: AdminPayment[];
  totalCount: number;
  summary: PaymentSummary;
}
