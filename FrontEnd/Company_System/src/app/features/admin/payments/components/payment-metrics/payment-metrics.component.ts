import { ChangeDetectionStrategy, Component, input, computed } from '@angular/core';
import { HeroIcon } from '../../../../../core/constants/hero-icon-d';
import { StatusCardsComponent } from '../../../../../shared/components/status-cards/status-cards.component';
import { StatusData } from '../../../../../shared/components/status-cards/status-cards.model';
import { PaymentSummary } from '../../../../../core/dto/payments/payment-summary-dto';

@Component({
  selector: 'app-payment-metrics',
  imports: [StatusCardsComponent],
  templateUrl: './payment-metrics.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaymentMetricsComponent {
  readonly summary = input.required<PaymentSummary>();
  protected readonly statuses = computed<StatusData[]>(() => {
    const summary = this.summary();
    return [
      {
        label: 'Pending Approval',
        value: summary.pendingAmount,
        format: 'currency',
        detailDivider: true,
        detail: `${summary.pendingCount} items awaiting review`,
        color: 'bg-blue-tint text-secondary-blue',
        icon: HeroIcon.pending,
      },
      {
        label: 'Approved Disbursements',
        value: summary.approvedAmount,
        format: 'currency',
        detailDivider: true,
        detail: `${summary.approvedCount} approved payments`,
        color: 'bg-turquoise-tint text-turquoise-text',
        icon: HeroIcon.check_circle,
      },
      {
        label: 'Rejected Payments',
        value: `${summary.rejectedCount} Payments`,
        detailDivider: true,
        detail: `${new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(summary.rejectedAmount)} rejected`,
        color: 'bg-error-bg text-error-text',
        icon: HeroIcon.xMark,
      },
      {
        label: 'Total Payments',
        value: summary.totalCount,
        detailDivider: true,
        detail: 'Payments across all states',
        color: 'bg-surface-high text-corporate-blue',
        icon: HeroIcon.banknotes,
      },
    ];
  });
}
