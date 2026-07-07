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
import { DropDownMenuComponent } from '../../shared/components/drop-down-menu/drop-down-menu.component';
import { AuthService } from '../../core/services/api/Auth-api-service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, IsVisableDirective, DatePipe, LoadingComponent, DropDownMenuComponent],
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
  protected readonly authService = inject(AuthService);

  // signals
  currApproval = signal<'toApprove' | 'requested'>('toApprove');
  showActivityStatusMenuFor = signal<string>('');

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

  ToggleShowActivityStatusMenu(showForId: string) {
    this.showActivityStatusMenuFor.update(curr => !!curr ? '' : showForId);
  }

  // on select new task status
  updateTaskStatus(taskId: string, newStatusIdx: number, currStatus: TaskStatusEnum) {
    this.showActivityStatusMenuFor.set('');

    if (currStatus == newStatusIdx) return; // TODO show toast

    this.tasksService.updateTaskStatus(taskId, newStatusIdx);
  }
}
