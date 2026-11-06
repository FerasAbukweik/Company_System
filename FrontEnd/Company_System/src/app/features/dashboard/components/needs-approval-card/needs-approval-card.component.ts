import { ApprovalStatusEnum } from '../../../../core/enums/approval-state-enum';
import { ToastService } from '../../../../core/services/api-services/toast-service';
import { Component, inject, input } from '@angular/core';
import { ToApproveDTO } from '../../../../core/dto/approval/to-approve-dto';
import { DatePipe } from '@angular/common';
import { ApprovalService } from '../../../../core/services/api-services/approval-service';

@Component({
  selector: 'app-needs-approval-card',
  imports: [DatePipe],
  templateUrl: './needs-approval-card.component.html',
  host: {
    class:
      'p-4 rounded-lg border border-outline-light hover:shadow-md transition-all bg-surface-lowest relative overflow-hidden group',
  },
})
export class NeedsApprovalCardComponent {
  private readonly toastService = inject(ToastService);

  // DI
  protected readonly approvalService = inject(ApprovalService);

  // input
  approval = input.required<ToApproveDTO>();

  updateStatus(newStatus: ApprovalStatusEnum) {
    this.approvalService.updateStatus(this.approval().id, newStatus).subscribe({
      next: () => {
        this.toastService.success('updated approval successfully');
      },
      error: () => {},
    });
  }
}
