import { Component, input, output } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { DrawerComponent } from '../../../../shared/components/drawer/drawer.component';
import { AdminPayment } from '../../../../core/dto/payments/admin-payment-dto';
import { PaymentStateBadgeComponent } from '../../../admin/payments/components/payment-state-badge/payment-state-badge.component';

@Component({
  selector: 'app-payslip-drawer',
  imports: [DrawerComponent, CurrencyPipe, DatePipe, PaymentStateBadgeComponent],
  templateUrl: './payslip-drawer.component.html',
})
export class PayslipDrawerComponent {
  readonly record = input<AdminPayment | null>(null);
  readonly closed = output<void>();
  readonly printRequested = output<void>();
}
