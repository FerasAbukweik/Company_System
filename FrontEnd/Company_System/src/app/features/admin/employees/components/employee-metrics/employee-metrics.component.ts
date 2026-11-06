import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { StatusCardsComponent } from '../../../../../shared/components/status-cards/status-cards.component';
import { StatusData } from '../../../../../shared/components/status-cards/status-cards.model';
import { EmployeesSummary } from '../../../../../core/dto/users/employees-summary-dto';

@Component({
  selector: 'app-employee-metrics',
  imports: [StatusCardsComponent],
  templateUrl: './employee-metrics.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmployeeMetricsComponent {
  readonly summary = input.required<EmployeesSummary>();

  protected readonly statuses = computed<StatusData[]>(() => {
    const summary = this.summary();
    return [
      { label: 'Employees', value: summary.headcount },
      { label: 'Active', value: summary.active },
      { label: 'Inactive', value: summary.headcount - summary.active },
      {
        label: 'Combined Hourly Rates',
        value: summary.totalHourlyRates,
        format: 'currency',
        detail: `${summary.configuredHourlyRates} hourly rates configured`,
      },
    ];
  });
}
