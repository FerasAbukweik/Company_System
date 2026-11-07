import { inject, Injectable, signal } from '@angular/core';
import { defer, of, tap, Observable, finalize, shareReplay } from 'rxjs';
import { ToastService } from './toast-service';
import { toastError } from '../../utils/toast-error';
import { UsersApiService } from '../api/users-api-service';
import { EmployeesSummary } from '../../dto/users/employees-summary-dto';
import { EmployeePage } from '../../dto/users/employee-page-dto';
import { EmployeeQuery } from '../../dto/users/employee-query-dto';
import { UserDTO } from '../../dto/auth/user-dto';
import { AddEmployeeDTO } from '../../dto/users/add-employee-dto';
import { EmployeeFormValue } from '../../../features/admin/employees/employees.model';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly api = inject(UsersApiService);
  private readonly toastService = inject(ToastService);
  private employeePages: Record<string, EmployeePage> | null = null;
  private profile: UserDTO | null = null;
  private profileRequest: Observable<UserDTO> | null = null;
  private version = 0;
  private readonly _employeeSummary = signal<EmployeesSummary | null>(null);
  public readonly employeeSummary = this._employeeSummary.asReadonly();

  getEmployeeSummary() {
    return defer(() => {
      const cached = this._employeeSummary();
      if (cached !== null) return of(cached);
      const version = this.version;
      return this.api.employeeSummary().pipe(
        tap((summary) => {
          if (version === this.version) this._employeeSummary.set(summary);
        }),
      );
    });
  }

  employeePage(query: EmployeeQuery) {
    return defer(() => {
      const key = JSON.stringify([
        query.taken,
        query.sectionSize,
        query.search?.trim() ?? '',
        query.activeOnly ?? false,
      ]);
      const cached = this.employeePages?.[key];
      if (cached !== undefined) return of(cached);
      const version = this.version;

      return this.api.employeePage(query).pipe(
        tap((page) => {
          if (version !== this.version) return;
          this.employeePages ??= {};
          this.employeePages[key] = page;
        }),
      );
    });
  }

  me(skipRecovery = false) {
    return defer(() => {
      if (this.profile !== null) return of(this.profile);
      if (this.profileRequest !== null) return this.profileRequest;
      const version = this.version;

      this.profileRequest = this.api.me(skipRecovery).pipe(
        tap((profile) => {
          if (version === this.version) this.profile = profile;
        }),
        finalize(() => {
          if (version === this.version) this.profileRequest = null;
        }),
        shareReplay({ bufferSize: 1, refCount: true }),
      );

      return this.profileRequest;
    });
  }

  cacheProfile(profile: UserDTO) {
    this.profile = profile;
  }

  saveEmployee(value: EmployeeFormValue, id?: string) {
    return this.api.saveEmployee(value, id).pipe(tap(() => this.reset()));
  }

  addEmployee(value: AddEmployeeDTO) {
    return this.api.addEmployee(value).pipe(
      toastError(this.toastService, 'تعذر إضافة الموظف'),
      tap(() => {
        this.reset();
        this.toastService.success('Employee created successfully.');
      }),
    );
  }

  deactivate(id: string) {
    return this.api.deactivate(id).pipe(tap(() => this.reset()));
  }

  reset() {
    this.version++;
    this.employeePages = null;
    this._employeeSummary.set(null);
    this.profile = null;
    this.profileRequest = null;
  }
}
