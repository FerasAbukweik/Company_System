import { DebounceDirective } from '../../../../../shared/directives/debounce.directive';
import { ChangeDetectionStrategy, Component, output, model } from '@angular/core';
import { HeroIcon } from '../../../../../core/constants/hero-icon-d';
import { IconComponent } from '../../../../../shared/components/icon/icon.component';

/** Title, employee search and the "Add New Employee" button. */
@Component({
  selector: 'app-employee-toolbar',
  imports: [IconComponent, DebounceDirective],
  templateUrl: './employee-toolbar.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'p-6 flex flex-col lg:flex-row lg:items-center justify-between gap-4',
  },
})
export class EmployeeToolbarComponent {
  protected readonly HeroIcon = HeroIcon;

  // two-way bound filters
  search = model.required<string>();

  // output
  addEmployee = output<void>();
}
