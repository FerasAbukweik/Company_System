import { AdminPayment } from './admin-payment-dto';

export interface UserPaymentPage {
  items: AdminPayment[];
  totalCount: number;
}
