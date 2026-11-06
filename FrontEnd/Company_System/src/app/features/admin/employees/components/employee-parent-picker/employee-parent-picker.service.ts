import { apiError } from '../../../../../core/utils/api-error';
import { inject, Injectable, signal } from '@angular/core';
import { defer, finalize, of, tap } from 'rxjs';
import { UsersService } from '../../../../../core/services/api-services/users-service';
import { EmployeeDTO } from '../../../../../core/dto/users/employee-dto';
import { EmployeePage } from '../../../../../core/dto/users/employee-page-dto';

@Injectable()
export class EmployeeParentPickerService {
  private readonly usersService = inject(UsersService);
  private pageCache: Record<string, EmployeePage> | null = null;
  private search = '';
  private taken = 0;
  private totalCount = Infinity;

  readonly employees = signal<EmployeeDTO[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');

  reset(search: string) {
    this.search = search.trim();
    this.taken = 0;
    this.totalCount = Infinity;
    this.employees.set([]);
    this.error.set('');
  }

  loadMore() {
    return defer(() => {
      if (this.loading() || this.taken >= this.totalCount) return of(null);

      const search = this.search;
      const taken = this.taken;
      const key = JSON.stringify([search, taken]);
      const cached = this.pageCache?.[key];
      this.loading.set(true);
      this.error.set('');

      return (
        cached
          ? of(cached)
          : this.usersService.employeePage({ search, taken, sectionSize: 10, activeOnly: true })
      ).pipe(
        tap({
          next: (page) => {
            this.pageCache ??= {};
            this.pageCache[key] = page;
            if (search !== this.search) return;

            this.employees.update((employees) => [...employees, ...page.items]);
            this.taken += page.items.length;
            this.totalCount = page.items.length ? page.totalCount : this.taken;
          },
          error: (error) => this.error.set(apiError(error)),
        }),
        finalize(() => this.loading.set(false)),
      );
    });
  }
}
