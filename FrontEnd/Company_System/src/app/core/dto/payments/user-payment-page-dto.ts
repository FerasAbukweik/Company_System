import { AdminPayment } from './admin-payment-dto';

export interface UserPaymentPage {
  items: AdminPayment[];
  summary: { totalCount: number; approvedAmount: number; pendingAmount: number };
  latestApproved: AdminPayment | null;
}
