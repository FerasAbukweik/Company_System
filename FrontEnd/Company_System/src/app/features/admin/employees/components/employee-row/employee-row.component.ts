import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { PositionsEnum } from '../../../../../core/enum/positions-enum';
import { HeroIcon } from '../../../../../core/constants/hero-icon-d';
import { AvatarComponent } from '../../../../../shared/components/avatar/avatar.component';
import { DropDownMenuComponent } from '../../../../../shared/components/drop-down-menu/drop-down-menu.component';
import { IconComponent } from '../../../../../shared/components/icon/icon.component';
import { EmployeeDTO } from '../../../../../core/dto/users/employee-dto';
import { RowAction } from '../../employees.model';

const ROW_ACTIONS: { label: string; action: RowAction }[] = [
  { label: 'Adjust Hourly Rate', action: 'hourlyRate' },
  { label: 'View Profile', action: 'profile' },
  { label: 'Terminate Member', action: 'terminate' },
];

@Component({
  selector: 'tr[app-employee-row]',
  imports: [CurrencyPipe, AvatarComponent, DropDownMenuComponent, IconComponent],
  templateUrl: './employee-row.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'hover:bg-surface-low/70 transition-colors',
    '(document:click)': 'closeMenu()',
  },
})
export class EmployeeRowComponent {
  protected readonly HeroIcon = HeroIcon;
  protected readonly menuOptions = ROW_ACTIONS.map((item) => item.label);

  // inputs
  employee = input.required<EmployeeDTO>();
  selected = input<boolean>(false);
  /** Open the actions menu upwards (used for the last rows so it is not clipped). */
  menuOpensUp = input<boolean>(false);

  // outputs
  selectedChange = output<boolean>();
  edit = output<void>();
  action = output<RowAction>();

  // signals
  protected readonly isMenuOpen = signal(false);

  // computed
  protected readonly positionLabel = computed(() => PositionsEnum[this.employee().position]);

  // methods
  protected toggleMenu(event: Event) {
    event.stopPropagation(); // keep the document click handler from closing it right away
    this.isMenuOpen.update((open) => !open);
  }

  protected closeMenu() {
    if (this.isMenuOpen()) this.isMenuOpen.set(false);
  }

  protected onMenuOption(index: number) {
    this.closeMenu();
    this.action.emit(ROW_ACTIONS[index].action);
  }
}
