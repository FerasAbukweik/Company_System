import { ChangeDetectionStrategy, Component, input, output, computed } from '@angular/core';

@Component({
  selector: 'app-payment-pagination',
  imports: [],
  templateUrl: './payment-pagination.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaymentPaginationComponent {
  readonly page = input.required<number>();
  readonly size = input.required<number>();
  readonly total = input.required<number>();
  readonly pages = input.required<number>();
  readonly changed = output<number>();

  protected readonly end = computed(() => Math.min(this.page() * this.size(), this.total()));
}
