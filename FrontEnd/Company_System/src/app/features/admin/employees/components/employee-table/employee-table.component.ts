import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { EmployeeDTO } from '../../../../../core/dto/users/employee-dto';
import { RowAction } from '../../employees.model';
import { EmployeeRowComponent } from '../employee-row/employee-row.component';

export interface EmployeeRowAction {
  employee: EmployeeDTO;
  action: RowAction;
}

/** The directory table. Owns the row selection (checkboxes). */
@Component({
  selector: 'app-employee-table',
  imports: [EmployeeRowComponent],
  templateUrl: './employee-table.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    // min-height keeps the last rows' action menu from being clipped on short pages
    class: 'block w-full overflow-x-auto min-h-72',
  },
})
export class EmployeeTableComponent {
  // inputs
  employees = input.required<EmployeeDTO[]>();

  // outputs
  edit = output<EmployeeDTO>();
  rowAction = output<EmployeeRowAction>();

  // signals
  private readonly selectedIds = signal<ReadonlySet<string>>(new Set());

  // computed
  protected readonly isAllSelected = computed(() => {
    const employees = this.employees();
    const selected = this.selectedIds();
    return employees.length > 0 && employees.every((employee) => selected.has(employee.id));
  });

  // methods
  protected isSelected(id: string) {
    return this.selectedIds().has(id);
  }

  protected opensMenuUp(index: number) {
    const total = this.employees().length;
    return total > 3 && index >= total - 2;
  }

  protected toggleOne(id: string, checked: boolean) {
    this.selectedIds.update((current) => {
      const next = new Set(current);
      if (checked) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  protected toggleAll(checked: boolean) {
    this.selectedIds.update((current) => {
      const next = new Set(current);
      for (const employee of this.employees()) {
        if (checked) next.add(employee.id);
        else next.delete(employee.id);
      }
      return next;
    });
  }
}
