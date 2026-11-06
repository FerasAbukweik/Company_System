import { ChangeDetectionStrategy, Component, input, computed, output } from '@angular/core';
import { HeroIcon } from '../../../../../core/constants/hero-icon-d';
import { IconComponent } from '../../../../../shared/components/icon/icon.component';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { AdminPayment } from '../../../../../core/dto/payments/admin-payment-dto';
import { PaymentState } from '../../../../../core/enum/payment-state';
import { PaymentDecision } from '../../payments.model';
import { AvatarComponent } from '../../../../../shared/components/avatar/avatar.component';
import { PaymentStateBadgeComponent } from '../payment-state-badge/payment-state-badge.component';

@Component({
  selector: 'app-payment-table',
  imports: [IconComponent, AvatarComponent, CurrencyPipe, DatePipe, PaymentStateBadgeComponent],
  templateUrl: './payment-table.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaymentTableComponent {
  readonly payments = input.required<readonly AdminPayment[]>();
  readonly selected = input.required<ReadonlySet<string>>();
  readonly title = input.required<string>();

  readonly toggled = output<string>();
  readonly togglePage = output<boolean>();
  readonly decided = output<PaymentDecision>();
  readonly viewed = output<AdminPayment>();

  protected readonly HeroIcon = HeroIcon;
  protected readonly State = PaymentState;
  protected readonly pending = computed(() =>
    this.payments().filter((payment) => payment.state === PaymentState.Pending),
  );
  protected readonly allSelected = computed(
    () =>
      this.pending().length > 0 &&
      this.pending().every((payment) => this.selected().has(payment.id)),
  );
  protected readonly someSelected = computed(() =>
    this.pending().some((payment) => this.selected().has(payment.id)),
  );

  protected payrollDate(payment: AdminPayment) {
    return new Date(payment.year, payment.month - 1, 1);
  }
}
