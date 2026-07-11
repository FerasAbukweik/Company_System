import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Urls } from '../../constants/urls';
import { AddEmployeeDTO } from '../../dto/create-account-dto';
import { from } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AccountApiService {
  // DI
  private readonly http = inject(HttpClient);

  // private
  private readonly url = Urls.api + '/Account';

  // api calls

  addEmployee(toCreate: AddEmployeeDTO) {
    let form = new FormData();

    Object.entries(toCreate).forEach(([key, val]) => {
      form.append(key, val);
    });

    return this.http.post(this.url + '/AddEmployee', form);
  }
}
