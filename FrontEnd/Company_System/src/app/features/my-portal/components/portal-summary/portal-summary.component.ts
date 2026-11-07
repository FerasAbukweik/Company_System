import { Component, computed, input } from '@angular/core';
import { StatusCardsComponent } from '../../../../shared/components/status-cards/status-cards.component';
import { StatusData } from '../../../../shared/components/status-cards/status-cards.model';
import { UserDTO } from '../../../../core/dto/auth/user-dto';

@Component({
  selector: 'app-portal-summary',
  imports: [StatusCardsComponent],
  templateUrl: './portal-summary.component.html',
})
export class PortalSummaryComponent {
  readonly profile = input<UserDTO | null>(null);
  readonly approvedAmount = input(0);
  readonly pendingAmount = input(0);
  readonly paidLeaveDays = input(0);
  protected readonly remaining = computed(() =>
    this.profile()?.annualHolidayAllowance == null
      ? null
      : Math.max(0, this.profile()!.annualHolidayAllowance! - this.paidLeaveDays()),
  );

  protected readonly statuses = computed<StatusData[]>(() => {
    const hourlyRate = this.profile()?.hourlyRate;
    const remaining = this.remaining();
    return [
      {
        label: 'Hourly Rate',
        value: hourlyRate ?? 'Not configured',
        format: hourlyRate == null ? undefined : 'currency',
      },
      { label: 'Pending Payments', value: this.pendingAmount(), format: 'currency' },
      {
        label: 'Holidays',
        value: `${this.paidLeaveDays()} days taken`,
        description:
          remaining === null ? 'Allowance not configured' : `${remaining} days remaining`,
        detail: 'Based on recorded paid leave this year.',
      },
      { label: 'Approved Payments This Year', value: this.approvedAmount(), format: 'currency' },
    ];
  });
}
