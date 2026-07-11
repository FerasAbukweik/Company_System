import { Component, inject, input } from '@angular/core';
import { ToApproveDTO } from '../../../../core/dto/to-approve-dto';
import { DatePipe } from '@angular/common';
import { ApprovalService } from '../../../../core/services/client/approval-service';

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
  // DI
  protected readonly approvalService = inject(ApprovalService);

  // input
  approval = input.required<ToApproveDTO>();
}
