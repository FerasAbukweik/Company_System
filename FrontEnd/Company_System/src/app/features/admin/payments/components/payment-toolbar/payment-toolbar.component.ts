import { DebounceDirective } from '../../../../../shared/directives/debounce.directive';
import { ChangeDetectionStrategy, Component, model, input, output } from '@angular/core';
import { HeroIcon } from '../../../../../core/constants/hero-icon-d';
import { IconComponent } from '../../../../../shared/components/icon/icon.component';

@Component({
  selector: 'app-payment-toolbar',
  imports: [IconComponent, DebounceDirective],
  templateUrl: './payment-toolbar.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaymentToolbarComponent {
  protected readonly HeroIcon = HeroIcon;
  protected readonly months = [
    'January',
    'February',
    'March',
    'April',
    'May',
    'June',
    'July',
    'August',
    'September',
    'October',
    'November',
    'December',
  ];

  readonly search = model.required<string>();
  readonly month = model.required<number>();
  readonly year = model.required<number>();
  readonly years = input.required<readonly number[]>();
  readonly exported = output<void>();
}
