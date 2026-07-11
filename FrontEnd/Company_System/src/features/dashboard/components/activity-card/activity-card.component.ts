import { Component, input } from '@angular/core';
import { ActivityDTO } from '../../../../core/dto/activity-dto';
import { ActivityTypeEnum } from '../../../../core/enums/activity-type-enum';
import { HeroIcon } from '../../../../core/constants/hero-icon-d';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-activity-card',
  imports: [DatePipe],
  templateUrl: './activity-card.component.html',
  host: {
    class: 'relative pl-12',
  },
})
export class ActivityCardComponent {
  // input
  activity = input.required<ActivityDTO>();

  // methods

  getActivityIcon(type: ActivityTypeEnum) {
    if (this.isActivityTypeCompleted(type)) return HeroIcon.check_circle;

    if (this.isActivityTypePending(type)) return HeroIcon.pending;

    if (this.isActivityTypeRejected(type)) return HeroIcon.alert;

    return HeroIcon.alert;
  }

  private isActivityTypeCompleted(type: ActivityTypeEnum) {
    return (
      type == ActivityTypeEnum.ApprovalApproved ||
      type == ActivityTypeEnum.TaskAdded ||
      type == ActivityTypeEnum.TaskCompleted
    );
  }

  private isActivityTypePending(type: ActivityTypeEnum) {
    return type == ActivityTypeEnum.ApprovalPending || type == ActivityTypeEnum.TaskPendingApproval;
  }

  private isActivityTypeRejected(type: ActivityTypeEnum) {
    return (
      type == ActivityTypeEnum.ApprovalRejected ||
      type == ActivityTypeEnum.MissingType ||
      type == ActivityTypeEnum.TaskRejected
    );
  }
}
