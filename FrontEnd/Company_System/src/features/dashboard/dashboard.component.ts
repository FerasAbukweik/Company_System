import { Component, inject, signal } from '@angular/core';
import { TasksService } from '../../core/services/client/tasks-service';
import { IsVisableDirective } from '../../shared/directives/is-visable.directive';
import { ApprovalService } from '../../core/services/client/approval-service';
import { ActivitiesService } from '../../core/services/client/activities-service';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { TaskCardComponent } from './components/task-card/task-card.component';
import { TaskCardService } from './components/task-card/task-card.service';
import { NeedsApprovalCardComponent } from './components/needs-approval-card/needs-approval-card.component';
import { RequestedApprovalCardComponent } from './components/requested-approval-card/requested-approval-card.component';
import { ActivityCardComponent } from './components/activity-card/activity-card.component';
import { AuthService } from '../../core/services/client/auth-service';
import { TaskDTO } from '../../core/dto/task-dto';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    IsVisableDirective,
    LoadingComponent,
    TaskCardComponent,
    NeedsApprovalCardComponent,
    RequestedApprovalCardComponent,
    ActivityCardComponent,
  ],
  providers: [TaskCardService],
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
  protected readonly taskCardService = inject(TaskCardService);

  // signals
  currApproval = signal<'toApprove' | 'requested'>('toApprove');
  showTaskDescription = signal<TaskDTO | null>(null);

  // methods

  // change approval page
  chaneApproval(changeTo: 'toApprove' | 'requested') {
    if (changeTo == this.currApproval()) return;

    this.currApproval.set(changeTo);
  }

  // reset approval
  resetApproval() {
    this.approvalService.resetRequested();
    this.approvalService.resetToApprova();

    if (this.currApproval() == 'requested') this.approvalService.loadMoreRequestedApprovals();
    if (this.currApproval() == 'toApprove') this.approvalService.loadMoreToApprove();
  }

  hideTaskDescription() {
    this.showTaskDescription.set(null);
  }
}
