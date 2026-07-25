import { inject, Injectable } from '@angular/core';
import { AccountApiService } from '../api/account-api-service';
import { ToastService } from './toast-service';
import { AddEmployeeDTO } from '../../dto/create-account-dto';
import { catchError, firstValueFrom, tap, throwError } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AccountService {
  // DI
  private readonly _accountApiService = inject(AccountApiService);
  private readonly _toastService = inject(ToastService);

  // methods

  addEmployee(toAddEmployee: AddEmployeeDTO) {
    return this._accountApiService.addEmployee(toAddEmployee).pipe(
      tap(() => this._toastService.success('Account Created Successfully')),
      catchError((err) => {
        this._toastService.error('something went wrong createing account');
        return throwError(() => err);
      }),
    );
  }
}
