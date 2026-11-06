import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { IconComponent } from '../icon/icon.component';
import { StatusData } from './status-cards.model';

@Component({
  selector: 'app-status-cards',
  imports: [CurrencyPipe, IconComponent],
  templateUrl: './status-cards.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
})
export class StatusCardsComponent {
  readonly statuses = input.required<readonly StatusData[]>();
  readonly label = input('Summary');
}
