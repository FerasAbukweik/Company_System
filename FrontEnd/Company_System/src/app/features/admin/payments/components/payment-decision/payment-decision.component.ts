import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
  computed,
  effect,
  viewChild,
  ElementRef,
} from '@angular/core';
import { HeroIcon } from '../../../../../core/constants/hero-icon-d';
import { IconComponent } from '../../../../../shared/components/icon/icon.component';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { AdminPayment } from '../../../../../core/dto/payments/admin-payment-dto';
import { PaymentState } from '../../../../../core/enum/payment-state';
import { PaymentDecision } from '../../payments.model';

@Component({
  selector: 'app-payment-decision',
  imports: [IconComponent, CurrencyPipe, DatePipe],
  templateUrl: './payment-decision.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaymentDecisionComponent {
  readonly saving = input(false);
  readonly error = input('');
  readonly decision = input<PaymentDecision | null>(null);
  readonly closed = output<void>();
  readonly confirmed = output<PaymentDecision>();

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  protected readonly State = PaymentState;
  protected readonly HeroIcon = HeroIcon;
  protected readonly total = computed(
    () => this.decision()?.payments.reduce((sum, payment) => sum + payment.paidAmount, 0) ?? 0,
  );

  constructor() {
    effect(() => {
      const dialog = this.dialog().nativeElement;
      if (this.decision() && !dialog.open) dialog.showModal();
      else if (!this.decision() && dialog.open) dialog.close();
    });
  }

  protected payrollDate(payment: AdminPayment) {
    return new Date(payment.year, payment.month - 1, 1);
  }

  protected cancel(event: Event) {
    event.preventDefault();
    this.closed.emit();
  }

  protected backdropClick(event: MouseEvent) {
    const dialog = this.dialog().nativeElement;
    const box = dialog.getBoundingClientRect();
    if (
      event.target === dialog &&
      (event.clientX < box.left ||
        event.clientX > box.right ||
        event.clientY < box.top ||
        event.clientY > box.bottom)
    )
      this.closed.emit();
  }
}
