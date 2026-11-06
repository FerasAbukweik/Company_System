import { PaymentState } from '../../enum/payment-state';

export interface AdminPayment {
  id: string;
  userId: string;
  employeeName: string;
  imageUrl: string | null;
  year: number;
  month: number;
  paidAmount: number;
  paymentDate: string | null;
  state: PaymentState;
  account: string | null;
}
