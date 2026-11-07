import { AdminPayment } from './admin-payment-dto';

export interface PaymentPage {
  items: AdminPayment[];
  totalCount: number;
}
