import { Component, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { TasksService } from '../../core/services/client/tasks-service';
import { IsVisableDirective } from '../../shared/directives/is-visable.directive';
import { TaskStatusEnum } from '../../core/enum/task-states-enum';
import { ApprovalService } from '../../core/services/client/approval-service';
import { ApprovalStatusEnum } from '../../core/enums/approval-state-enum';
import { ActivitiesService } from '../../core/services/client/activities-service';
import { ActivityTypeEnum } from '../../core/enums/activity-type-enum';
import { HeroIcon } from '../../core/constants/hero-icon-d';
import { LoadingComponent } from '../../shared/components/loading/loading.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, IsVisableDirective, DatePipe, LoadingComponent],
  templateUrl: './dashboard.component.html',
  host: {
    class: 'text-text-primary min-h-screen font-sans p-4 block',
  },
})
export class DashboardComponent {
  // DI
  protected readonly tasksService = inject(TasksService);
  protected readonly approvalService = inject(ApprovalService);
  protected readonly activitiesService = inject(ActivitiesService);

  // signals
  protected currApproval = signal<'toApprove' | 'requested'>('toApprove');

  // methods

  // convert TaskStatusEnum to string
  getStatusString(val: TaskStatusEnum) {
    return TaskStatusEnum[val];
  }

  // convert ApprovalStatusEnum to string
  toApprovalStatusString(status: ApprovalStatusEnum) {
    return ApprovalStatusEnum[status];
  }

  // change approval page
  chaneApproval(changeTo: 'toApprove' | 'requested') {
    if (changeTo == this.currApproval()) return;

    this.currApproval.set(changeTo);
  }

  // generate approval count string based on current page
  generateApprovalCountString() {
    if (this.currApproval() == 'requested')
      return this.approvalService.getRequestedApprovalsCount() + ' Requested';

    if (this.currApproval() == 'toApprove')
      return this.approvalService.getToApproveCount() + ' Needs Approval';

    return 'Unhandled State';
  }

  // get sutable activity icon ---------------------------------------------------------

  getActivityIcon(type: ActivityTypeEnum) {
    if (this.isActivityTypeCompleted(type)) return HeroIcon.check_circle;

    if (this.isActivityTypePending(type)) return HeroIcon.pending;

    if (this.isActivityTypeRejected(type)) return HeroIcon.alert;

    return HeroIcon.alert;
  }

  private isActivityTypeCompleted(type: ActivityTypeEnum) {
    return (
      type == ActivityTypeEnum.ApprovalApproved ||
      ActivityTypeEnum.TaskAdded ||
      ActivityTypeEnum.TaskCompleted
    );
  }

  private isActivityTypePending(type: ActivityTypeEnum) {
    return type == ActivityTypeEnum.ApprovalPending || ActivityTypeEnum.TaskPendingApproval;
  }

  private isActivityTypeRejected(type: ActivityTypeEnum) {
    return (
      type == ActivityTypeEnum.ApprovalRejected ||
      ActivityTypeEnum.MissingType ||
      ActivityTypeEnum.TaskRejected
    );
  }

  // get sutable activity icon ---------------------------------------------------------

  // reset approval
  resetApproval() {
    this.approvalService.reset();

    if (this.currApproval() == 'requested') this.approvalService.loadMoreRequestedApprovals();
    if (this.currApproval() == 'toApprove') this.approvalService.loadMoreToApprove();
  }
}
