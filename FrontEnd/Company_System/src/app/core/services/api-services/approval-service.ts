import { ToastService } from './toast-service';
import { toastError } from '../../utils/toast-error';
import { inject, Injectable, signal } from '@angular/core';
import { ApprovalApiService } from '../api/approval-api-service';
import { ToApproveDTO } from '../../dto/approval/to-approve-dto';
import { RequestedApprovalDTO } from '../../dto/approval/requested-approval-dto';
import { ApprovalStatusEnum } from '../../enums/approval-state-enum';
import { EMPTY, Subject, defer, finalize, takeUntil, tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ApprovalService {
  private readonly _toastService = inject(ToastService);

  // DI
  private readonly _approvalApiService = inject(ApprovalApiService);

  // signals
  private _isLoadingToApprove = signal<boolean>(false);
  private _isLoadingRequestedApprovals = signal<boolean>(false);
  private _toApprove = signal<ToApproveDTO[]>([]);
  private _requestedApprovals = signal<RequestedApprovalDTO[]>([]);

  // subject
  readonly cancelToApproveRequest$ = new Subject<void>();
  readonly cancelRequestedApprovalsRequest$ = new Subject<void>();

  // private
  readonly sectionSize = 10;
  private _isMoreToApproveAvaiable = true;
  private _isMoreRequestedAvaiable = true;

  // getters

  get isLoadingToApprove() {
    return this._isLoadingToApprove.asReadonly();
  }

  get isLoadingRequestedApprovals() {
    return this._isLoadingRequestedApprovals.asReadonly();
  }

  get toApprove() {
    return this._toApprove.asReadonly();
  }

  get requestedApprovals() {
    return this._requestedApprovals.asReadonly();
  }

  // methods

  resetToApprova() {
    this.cancelToApproveRequest$.next();

    this._toApprove.set([]);
    this._isLoadingToApprove.set(false);
    this._isMoreToApproveAvaiable = true;
  }
  resetRequested() {
    this.cancelRequestedApprovalsRequest$.next();

    this._requestedApprovals.set([]);
    this._isLoadingRequestedApprovals.set(false);
    this._isMoreRequestedAvaiable = true;
  }

  updateStatus(approvalId: string, newStatus: ApprovalStatusEnum) {
    return defer(() => {
      if (this._isLoadingToApprove()) return EMPTY;
      this._isLoadingToApprove.set(true);
      // before update data
      const oldData = [...this._toApprove()];

      // optimistic update
      this._toApprove.update((curr) => curr.filter((a) => a.id != approvalId));

      return this._approvalApiService.updateStatus(approvalId, newStatus).pipe(
        toastError(this._toastService, 'تعذر تحديث الموافقة'),
        takeUntil(this.cancelToApproveRequest$),
        tap({
          error: () => {
            this._toApprove.set(oldData);
          },
        }),
        finalize(() => {
          this._isLoadingToApprove.set(false);
        }),
      );
    });
  }

  loadMoreToApprove() {
    return defer(() => {
      if (this._isLoadingToApprove() || !this._isMoreToApproveAvaiable) return EMPTY;
      this._isLoadingToApprove.set(true);

      return this._approvalApiService
        .getToApprove({ taken: this._toApprove().length, sectionSize: this.sectionSize })
        .pipe(
          toastError(this._toastService, 'تعذر تحميل طلبات الموافقة'),
          takeUntil(this.cancelToApproveRequest$),
        )
        .pipe(
          tap({
            next: (data) => {
              this._toApprove.update((curr) => [...curr, ...data]);

              this._isMoreToApproveAvaiable = data.length > 0;
            },
          }),
          finalize(() => {
            this._isLoadingToApprove.set(false);
          }),
        );
    });
  }

  loadMoreRequestedApprovals() {
    return defer(() => {
      if (this._isLoadingRequestedApprovals() || !this._isMoreRequestedAvaiable) return EMPTY;
      this._isLoadingRequestedApprovals.set(true);

      return this._approvalApiService
        .getRequested({ taken: this._requestedApprovals().length, sectionSize: this.sectionSize })
        .pipe(
          toastError(this._toastService, 'تعذر تحميل الموافقات المطلوبة'),
          takeUntil(this.cancelRequestedApprovalsRequest$),
        )
        .pipe(
          tap({
            next: (data) => {
              this._requestedApprovals.update((curr) => [...curr, ...data]);

              this._isMoreRequestedAvaiable = data.length > 0;
            },
          }),
          finalize(() => {
            this._isLoadingRequestedApprovals.set(false);
          }),
        );
    });
  }

  requestHoliday() {
    return this._approvalApiService
      .RequestHoliday()
      .pipe(toastError(this._toastService, 'تعذر طلب الإجازة'));
  }
}
