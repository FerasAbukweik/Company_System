import { apiError } from '../../../core/utils/api-error';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  untracked,
  DestroyRef,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { ToastService } from '../../../core/services/api-services/toast-service';
import { EmployeeDrawerComponent } from './components/employee-drawer/employee-drawer.component';
import { EmployeeMetricsComponent } from './components/employee-metrics/employee-metrics.component';
import { EmployeePaginationComponent } from './components/employee-pagination/employee-pagination.component';
import {
  EmployeeRowAction,
  EmployeeTableComponent,
} from './components/employee-table/employee-table.component';
import { EmployeeToolbarComponent } from './components/employee-toolbar/employee-toolbar.component';

import { EmployeesAdminService } from './employees.service';
import { EmployeeDTO } from '../../../core/dto/users/employee-dto';
import { EmployeeFormValue } from './employees.model';

@Component({
  selector: 'app-employees',
  imports: [
    EmployeeMetricsComponent,
    EmployeeToolbarComponent,
    EmployeeTableComponent,
    EmployeePaginationComponent,
    EmployeeDrawerComponent,
  ],
  providers: [EmployeesAdminService],
  templateUrl: './employees.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block p-4 text-text-primary',
  },
})
export class EmployeesComponent {
  // DI
  protected readonly employeesService = inject(EmployeesAdminService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  // filters
  protected readonly search = signal('');
  protected readonly pageSize = signal(5);

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.employeesService.totalCount() / this.pageSize())),
  );

  // current page: goes back to 1 when a filter / the page size changes,
  // and is clamped when rows disappear (e.g. the last row of the last page is removed)
  protected readonly page = linkedSignal<{ filters: unknown[]; totalPages: number }, number>({
    source: () => ({
      filters: [this.search(), this.pageSize()],
      totalPages: this.totalPages(),
    }),
    computation: (source, previous) => {
      const filtersUnchanged = previous?.source.filters.every(
        (value, index) => value === source.filters[index],
      );
      return previous && filtersUnchanged ? Math.min(previous.value, source.totalPages) : 1;
    },
  });

  // drawer
  protected readonly isDrawerOpen = signal(false);
  protected readonly editedEmployee = signal<EmployeeDTO | null>(null);
  protected readonly isSaving = signal(false);
  protected readonly serverError = signal('');

  constructor() {
    effect(() => {
      const query = {
        taken: (this.page() - 1) * this.pageSize(),
        sectionSize: this.pageSize(),
        search: this.search(),
      };
      untracked(() => this.employeesService.load(query));
    });
  }

  // methods
  protected openCreate() {
    this.editedEmployee.set(null);
    this.openDrawer();
  }

  protected openEdit(employee: EmployeeDTO) {
    this.editedEmployee.set(employee);
    this.openDrawer();
  }

  protected closeDrawer() {
    this.isDrawerOpen.set(false);
  }

  /** Create (no employee being edited) or update, depending on how the drawer was opened. */
  protected onSave(value: EmployeeFormValue) {
    const employee = this.editedEmployee();
    const request$ = employee
      ? this.employeesService.update(employee.id, value)
      : this.employeesService.create(value);

    this.serverError.set('');
    this.isSaving.set(true);

    request$
      .pipe(
        finalize(() => this.isSaving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.closeDrawer();
          this.toastService.success(
            employee ? 'Employee updated successfully.' : 'Employee created successfully.',
          );
        },
        error: (err) => {
          this.serverError.set(apiError(err));
        },
      });
  }

  private openDrawer() {
    this.serverError.set('');
    this.isDrawerOpen.set(true);
  }

  protected onRowAction({ employee, action }: EmployeeRowAction) {
    if (action === 'terminate') {
      if (!window.confirm(`Deactivate ${employee.fullName} and their reporting branch?`)) return;
      this.employeesService
        .terminate(employee.id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => this.toastService.success('Employee branch deactivated.'),
          error: (error) => this.toastService.error(apiError(error)),
        });
    } else this.openEdit(employee);
  }
}
