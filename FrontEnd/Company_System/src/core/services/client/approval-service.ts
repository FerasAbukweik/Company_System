import { inject, Injectable, signal } from '@angular/core';
import { ApprovalApiService } from '../api/approval-api-service';
import { ToApproveDTO } from '../../dto/to-approve-dto';
import { RequestedApprovalDTO } from '../../dto/requested-approval-dto';
import { ApprovalStatusEnum } from '../../enums/approval-state-enum';
import { LazyDTO } from '../../dto/lazy-dto';
import { ToastService } from './toast-service';
import { Subject, takeUntil } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ApprovalService {
  // DI
  private readonly _approvalApiService = inject(ApprovalApiService);
  private readonly _toastService = inject(ToastService);

  // signals
  private _isLoadingToApprove = signal<boolean>(false);
  private _isLoadingRequestedApprovals = signal<boolean>(false);
  private _toApprove = signal<ToApproveDTO[]>([]);
  private _requestedApprovals = signal<RequestedApprovalDTO[]>([]);

  // subject
  readonly cancelToApproveRequest$ = new Subject<void>();
  readonly cancelRequestedApprovalsRequest$ = new Subject<void>();

  // private
  private _lazyDataForToApprove: LazyDTO = {
    taken: 0,
    sectionSize: 10,
  };
  private _lazyDataForRequested: LazyDTO = {
    taken: 0,
    sectionSize: 10,
  };
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
    this._lazyDataForToApprove.taken = 0;
    this._isMoreToApproveAvaiable = true;
  }
  resetRequested() {
    this.cancelRequestedApprovalsRequest$.next();

    this._requestedApprovals.set([]);
    this._isLoadingRequestedApprovals.set(false);
    this._lazyDataForRequested.taken = 0;
    this._isMoreRequestedAvaiable = true;
  }

  updateStatus(approvalId: string, newStatus: ApprovalStatusEnum) {
    if (this._isLoadingToApprove()) return;
    this._isLoadingToApprove.set(true);
    // before update data
    const oldData = [...this._toApprove()];

    // optimistic update
    this._toApprove.update((curr) => curr.filter((a) => a.id != approvalId));

    this._approvalApiService.updateStatus(approvalId, newStatus).subscribe({
      next: () => {
        this._toastService.success('updated approval successfully');
        this._isLoadingToApprove.set(false);
      },
      error: () => {
        this._toastService.error('something went wrong updating approval');

        this._toApprove.set(oldData);
        this._isLoadingToApprove.set(false);
      },
    });
  }

  loadMoreToApprove() {
    if (this._isLoadingToApprove() || !this._isMoreToApproveAvaiable) return;
    this._isLoadingToApprove.set(true);

    this._approvalApiService
      .getToApprove(this._lazyDataForToApprove)
      .pipe(takeUntil(this.cancelToApproveRequest$))
      .subscribe({
        next: (data) => {
          this._toApprove.update((curr) => [...curr, ...data]);

          this._lazyDataForToApprove.taken += data.length;
          this._isMoreToApproveAvaiable = data.length > 0;
          this._isLoadingToApprove.set(false);
        },
        error: () => {
          this._toastService.error('something went wrong while loading needs approval');
          this._isLoadingToApprove.set(false);
        },
      });
  }

  loadMoreRequestedApprovals() {
    if (this._isLoadingRequestedApprovals() || !this._isMoreRequestedAvaiable) return;
    this._isLoadingRequestedApprovals.set(true);

    this._approvalApiService
      .getRequested(this._lazyDataForRequested)
      .pipe(takeUntil(this.cancelRequestedApprovalsRequest$))
      .subscribe({
        next: (data) => {
          this._requestedApprovals.update((curr) => [...curr, ...data]);

          this._lazyDataForRequested.taken += data.length;
          this._isMoreRequestedAvaiable = data.length > 0;
          this._isLoadingRequestedApprovals.set(false);
        },
        error: () => {
          this._toastService.error('something went wrong while loading requested approvals');
          this._isLoadingRequestedApprovals.set(false);
        },
      });
  }

  requestHoliday() {
    this._approvalApiService.RequestHoliday().subscribe({
      next: () => {
        this._toastService.success('Holiday Requested Successfully');
      },
      error: () => {
        this._toastService.error('something went wrong requesting holiday');
      },
    });
  }
}
