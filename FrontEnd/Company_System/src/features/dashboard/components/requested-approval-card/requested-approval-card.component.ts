import { Component, inject, input } from '@angular/core';
import { RequestedApprovalDTO } from '../../../../core/dto/requested-approval-dto';
import { DatePipe } from '@angular/common';
import { ApprovalStatusEnum } from '../../../../core/enums/approval-state-enum';
import { ApprovalService } from '../../../../core/services/client/approval-service';

@Component({
  selector: 'app-requested-approval-card',
  imports: [DatePipe],
  templateUrl: './requested-approval-card.component.html',
  host: {
    class:
      'p-4 rounded-lg border border-outline-light hover:shadow-md transition-all bg-surface-lowest relative overflow-hidden',
  },
})
export class RequestedApprovalCardComponent {
  // input
  approval = input.required<RequestedApprovalDTO>();

  // convert ApprovalStatusEnum to string
  toApprovalStatusString(status: ApprovalStatusEnum) {
    return ApprovalStatusEnum[status];
  }
}
