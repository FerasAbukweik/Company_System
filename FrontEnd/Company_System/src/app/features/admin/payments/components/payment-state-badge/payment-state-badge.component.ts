import { ChangeDetectionStrategy, Component, input, computed } from '@angular/core';
import { PaymentState } from '../../../../../core/enum/payment-state';

@Component({
  selector: 'app-payment-state-badge',
  imports: [],
  templateUrl: './payment-state-badge.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaymentStateBadgeComponent {
  readonly state = input.required<PaymentState>();
  protected readonly label = computed(() => PaymentState[this.state()]);
  protected readonly classes = computed(() =>
    this.state() === PaymentState.Pending
      ? 'bg-blue-tint text-secondary-blue'
      : this.state() === PaymentState.Approved
        ? 'bg-turquoise-tint text-turquoise-text'
        : 'bg-error-bg text-error-text',
  );
}
