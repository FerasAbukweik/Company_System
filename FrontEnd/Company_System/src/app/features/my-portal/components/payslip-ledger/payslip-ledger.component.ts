import { Component, input, output } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { AdminPayment } from '../../../../core/dto/payments/admin-payment-dto';
import { PaymentStateBadgeComponent } from '../../../admin/payments/components/payment-state-badge/payment-state-badge.component';

@Component({
  selector: 'app-payslip-ledger',
  imports: [CurrencyPipe, DatePipe, PaymentStateBadgeComponent],
  templateUrl: './payslip-ledger.component.html',
})
export class PayslipLedgerComponent {
  readonly records = input<readonly AdminPayment[]>([]);
  readonly viewed = output<AdminPayment>();
}
