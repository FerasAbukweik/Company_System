import { apiError } from '../../../core/utils/api-error';
import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { defer, finalize, of, switchMap, tap, Subscription } from 'rxjs';
import { UsersService } from '../../../core/services/api-services/users-service';
import { EmployeeDTO } from '../../../core/dto/users/employee-dto';
import { EmployeePage } from '../../../core/dto/users/employee-page-dto';
import { EmployeeQuery } from '../../../core/dto/users/employee-query-dto';
import { EmployeesSummary } from '../../../core/dto/users/employees-summary-dto';
import { EmployeeFormValue } from './employees.model';

@Injectable()
export class EmployeesAdminService {
  private readonly usersService = inject(UsersService);
  private readonly destroyRef = inject(DestroyRef);
  private request?: Subscription;
  private query: EmployeeQuery = { taken: 0, sectionSize: 5 };
  private cacheScope: string | null = null;

  readonly employeePages = signal<Readonly<Record<number, EmployeeDTO[]>>>({});
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly summary = signal<EmployeesSummary>({
    headcount: 0,
    active: 0,
    totalHourlyRates: 0,
    configuredHourlyRates: 0,
  });

  constructor() {
    this.destroyRef.onDestroy(() => this.request?.unsubscribe());
  }

  private fetchPage(query: EmployeeQuery) {
    return defer(() => {
      const scope = JSON.stringify([query.search?.trim() ?? '', query.sectionSize]);
      if (scope !== this.cacheScope) {
        this.cacheScope = scope;
        this.employeePages.set({});
      }
      const page = Math.floor(query.taken / query.sectionSize) + 1;
      const cached = this.employeePages()[page];
      if (cached?.length) return of(cached);

      return this.usersService.employeePage(query).pipe(
        tap((result) => {
          if (scope !== this.cacheScope) return;
          this.publishPage(page, result);
        }),
      );
    });
  }

  private publishPage(page: number, result: EmployeePage) {
    this.employeePages.update((pages) => ({ ...pages, [page]: result.items }));
    this.totalCount.set(result.totalCount);
    this.summary.set(result.summary);
  }

  load(query: EmployeeQuery) {
    this.request?.unsubscribe();
    this.query = query;
    this.loading.set(true);
    this.error.set('');
    this.request = this.fetchPage(query)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        error: (error) => this.error.set(apiError(error)),
      });
  }

  reload() {
    this.usersService.reset();
    this.employeePages.set({});
    this.load(this.query);
  }

  private refresh() {
    this.employeePages.set({});
    return this.fetchPage(this.query);
  }
  create(value: EmployeeFormValue) {
    return this.usersService.saveEmployee(value).pipe(switchMap(() => this.refresh()));
  }
  update(id: string, value: EmployeeFormValue) {
    return this.usersService.saveEmployee(value, id).pipe(switchMap(() => this.refresh()));
  }
  terminate(id: string) {
    return this.usersService.deactivate(id).pipe(switchMap(() => this.refresh()));
  }
}
