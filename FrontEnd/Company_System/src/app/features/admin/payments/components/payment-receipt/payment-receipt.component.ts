import { ChangeDetectionStrategy, Component, input, output, computed } from '@angular/core';
import { HeroIcon } from '../../../../../core/constants/hero-icon-d';
import { IconComponent } from '../../../../../shared/components/icon/icon.component';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { AdminPayment } from '../../../../../core/dto/payments/admin-payment-dto';
import { DrawerComponent } from '../../../../../shared/components/drawer/drawer.component';
import { AvatarComponent } from '../../../../../shared/components/avatar/avatar.component';
import { PaymentStateBadgeComponent } from '../payment-state-badge/payment-state-badge.component';

@Component({
  selector: 'app-payment-receipt',
  imports: [
    IconComponent,
    CurrencyPipe,
    DatePipe,
    DrawerComponent,
    AvatarComponent,
    PaymentStateBadgeComponent,
  ],
  templateUrl: './payment-receipt.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaymentReceiptComponent {
  readonly payment = input<AdminPayment | null>(null);
  readonly closed = output<void>();

  protected readonly HeroIcon = HeroIcon;
  protected readonly payrollDate = computed(() => {
    const payment = this.payment();
    return payment ? new Date(payment.year, payment.month - 1, 1) : null;
  });
}
