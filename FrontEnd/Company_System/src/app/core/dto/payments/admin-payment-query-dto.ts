import { PaymentState } from '../../enum/payment-state';
import { LazyDTO } from '../lazy-loading/lazy-dto';

export interface AdminPaymentQuery extends LazyDTO {
  year: number;
  month?: number;
  state?: PaymentState | null;
  historyOnly?: boolean;
  fullName?: string;
}
