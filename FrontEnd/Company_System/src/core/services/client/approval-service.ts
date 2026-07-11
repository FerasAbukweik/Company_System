import { computed, inject, Injectable, signal } from '@angular/core';
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
  private readonly approvalApiService = inject(ApprovalApiService);
  private readonly toastService = inject(ToastService);

  // signals
  private isLoadingToApprove = signal<boolean>(false);
  private isLoadingRequestedApprovals = signal<boolean>(false);
  private toApprove = signal<ToApproveDTO[]>([]);
  private requestedApprovals = signal<RequestedApprovalDTO[]>([]);

  // subject
  private cancelToApproveRequest$ = new Subject<void>();
  private cancelRequestedApprovalsRequest$ = new Subject<void>();

  // private
  private lazyDataForToApprove: LazyDTO = {
    taken: 0,
    sectionSize: 10,
  };
  private lazyDataForRequested: LazyDTO = {
    taken: 0,
    sectionSize: 10,
  };
  private isMoreToApproveAvaiable = true;
  private isMoreRequestedAvaiable = true;

  // getters

  get getIsLoadingToApprove() {
    return this.isLoadingToApprove.asReadonly();
  }

  get getIsLoadingRequestedApprovals() {
    return this.isLoadingRequestedApprovals.asReadonly();
  }

  get getToApprove() {
    return this.toApprove.asReadonly();
  }

  get getRequestedApprovals() {
    return this.requestedApprovals.asReadonly();
  }

  // methods

  resetToApprova() {
    this.cancelToApproveRequest$.next();

    this.toApprove.set([]);
    this.isLoadingToApprove.set(false);
    this.lazyDataForToApprove.taken = 0;
    this.isMoreToApproveAvaiable = true;
  }
  resetRequested() {
    this.cancelRequestedApprovalsRequest$.next();

    this.requestedApprovals.set([]);
    this.isLoadingRequestedApprovals.set(false);
    this.lazyDataForRequested.taken = 0;
    this.isMoreRequestedAvaiable = true;
  }

  updateStatus(approvalId: string, newStatus: ApprovalStatusEnum) {
    if (this.isLoadingToApprove()) return;
    this.isLoadingToApprove.set(true);
    // before update data
    const oldData = [...this.toApprove()];

    // optimistic update
    this.toApprove.update((curr) => curr.filter((a) => a.id != approvalId));

    this.approvalApiService.updateStatus(approvalId, newStatus).subscribe({
      next: () => {
        this.toastService.success('updated approval successfully');
        this.isLoadingToApprove.set(false);
      },
      error: () => {
        this.toastService.error('something went wrong updating approval');

        this.toApprove.set(oldData);
        this.isLoadingToApprove.set(false);
      },
    });
  }

  loadMoreToApprove() {
    if (this.isLoadingToApprove() || !this.isMoreToApproveAvaiable) return;
    this.isLoadingToApprove.set(true);

    this.approvalApiService
      .getToApprove(this.lazyDataForToApprove)
      .pipe(takeUntil(this.cancelToApproveRequest$))
      .subscribe({
        next: (data) => {
          this.toApprove.update((curr) => [...curr, ...data]);

          this.lazyDataForToApprove.taken += data.length;
          this.isMoreToApproveAvaiable = data.length > 0;
          this.isLoadingToApprove.set(false);
        },
        error: () => {
          this.toastService.error('something went wrong while loading needs approval');
          this.isLoadingToApprove.set(false);
        },
      });
  }

  loadMoreRequestedApprovals() {
    if (this.isLoadingRequestedApprovals() || !this.isMoreRequestedAvaiable) return;
    this.isLoadingRequestedApprovals.set(true);

    this.approvalApiService
      .getRequested(this.lazyDataForRequested)
      .pipe(takeUntil(this.cancelRequestedApprovalsRequest$))
      .subscribe({
        next: (data) => {
          this.requestedApprovals.update((curr) => [...curr, ...data]);

          this.lazyDataForRequested.taken += data.length;
          this.isMoreRequestedAvaiable = data.length > 0;
          this.isLoadingRequestedApprovals.set(false);
        },
        error: () => {
          this.toastService.error('something went wrong while loading requested approvals');
          this.isLoadingRequestedApprovals.set(false);
        },
      });
  }

  requestHoliday() {
    this.approvalApiService.RequestHoliday().subscribe({
      next: () => {
        this.toastService.success('Holiday Requested Successfully');
      },
      error: () => {
        this.toastService.error('something went wrong requesting holiday');
      },
    });
  }
}
