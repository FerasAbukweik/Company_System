import { SKIP_AUTH_RECOVERY } from '../../constants/auth-http-context';
import { UserDTO } from '../../dto/auth/user-dto';
import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Urls } from '../../constants/urls';
import { map } from 'rxjs';
import { EmployeeDTO } from '../../dto/users/employee-dto';
import { EmployeePage } from '../../dto/users/employee-page-dto';
import { EmployeeQuery } from '../../dto/users/employee-query-dto';
import { AddEmployeeDTO } from '../../dto/users/add-employee-dto';
import { EmployeeFormValue } from '../../../features/admin/employees/employees.model';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly http = inject(HttpClient);
  private readonly base = Urls.api + '/Users';

  employeePage(query: EmployeeQuery) {
    return this.http.get<EmployeePage>(this.base + '/GetEmployees', {
      params: {
        taken: query.taken,
        sectionSize: query.sectionSize,
        ...(query.activeOnly ? { activeOnly: true } : {}),
        ...(query.search?.trim() ? { search: query.search.trim() } : {}),
      },
    });
  }

  employees(taken = 0, sectionSize = 20) {
    return this.employeePage({ taken, sectionSize }).pipe(map((page) => page.items));
  }

  me(skipRecovery = false) {
    return this.http.get<UserDTO>(this.base + '/Me', {
      context: new HttpContext().set(SKIP_AUTH_RECOVERY, skipRecovery),
    });
  }

  saveEmployee(value: EmployeeFormValue, id?: string) {
    const form = new FormData();
    const data = {
      fullName: value.fullName,
      userName: value.userName,
      email: value.email,
      phoneNumber: value.phone,
      position: value.position,
      hourlyRate: value.hourlyRate,
      annualHolidayAllowance: value.annualHolidayAllowance,
      image: value.image,
      ...(value.parentId ? { parentId: value.parentId } : {}),
      ...(id ? {} : { password: value.password }),
    };
    Object.entries(data).forEach(([key, value]) => {
      if (value !== null && value !== undefined)
        form.append(key, value instanceof Blob ? value : String(value));
    });
    return id
      ? this.http.put<EmployeeDTO>(this.base + '/UpdateEmployee/' + id, form)
      : this.http.post(this.base + '/AddEmployee', form);
  }

  deactivate(id: string) {
    return this.http.put(this.base + '/Deactivate/' + id, {});
  }

  addEmployee(toCreate: AddEmployeeDTO) {
    const form = new FormData();

    Object.entries(toCreate).forEach(([key, val]) => {
      form.append(key, val);
    });

    return this.http.post(this.base + '/AddEmployee', form);
  }
}
