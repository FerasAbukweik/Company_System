import { AdminPayment } from './admin-payment-dto';

export interface UserPaymentOverview {
  approvedAmount: number;
  pendingAmount: number;
  paidLeaveDays: number;
  latestApproved: AdminPayment | null;
}
