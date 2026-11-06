import { ToastService } from '../../core/services/api-services/toast-service';
import { Component, inject, OnDestroy, signal } from '@angular/core';
import { TasksService } from '../../core/services/api-services/tasks-service';
import { IsVisableDirective } from '../../shared/directives/is-visable.directive';
import { ApprovalService } from '../../core/services/api-services/approval-service';
import { ActivitiesService } from '../../core/services/api-services/activities-service';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { TaskCardComponent } from './components/task-card/task-card.component';
import { TaskCardService } from './components/task-card/task-card.service';
import { NeedsApprovalCardComponent } from './components/needs-approval-card/needs-approval-card.component';
import { RequestedApprovalCardComponent } from './components/requested-approval-card/requested-approval-card.component';
import { ActivityCardComponent } from './components/activity-card/activity-card.component';
import { AuthService } from '../../core/services/api-services/auth-service';
import { TaskDTO } from '../../core/dto/task/task-dto';
import { IconComponent } from '../../shared/components/icon/icon.component';
import { HeroIcon } from '../../core/constants/hero-icon-d';

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
    IconComponent,
  ],
  providers: [TaskCardService],
  templateUrl: './dashboard.component.html',
  host: {
    class: 'text-text-primary min-h-screen font-sans p-4 block',
  },
})
export class DashboardComponent implements OnDestroy {
  protected readonly HeroIcon = HeroIcon;

  private readonly toastService = inject(ToastService);

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

    if (this.currApproval() == 'requested') this.loadMoreRequestedApprovals();
    if (this.currApproval() == 'toApprove') this.loadMoreToApprove();
  }

  hideTaskDescription() {
    this.showTaskDescription.set(null);
  }

  // on destroy
  ngOnDestroy(): void {
    this.approvalService.cancelToApproveRequest$.next();
    this.approvalService.cancelRequestedApprovalsRequest$.next();
    this.tasksService.cancelRequest$.next();
    this.activitiesService.cancelRequest$.next();
  }

  loadMoreTasks() {
    this.tasksService.loadMore().subscribe({
      error: () => {},
    });
  }

  loadMoreActivities() {
    this.activitiesService.loadMore().subscribe({
      error: () => {},
    });
  }

  loadMoreToApprove() {
    this.approvalService.loadMoreToApprove().subscribe({
      error: () => {},
    });
  }

  loadMoreRequestedApprovals() {
    this.approvalService.loadMoreRequestedApprovals().subscribe({
      error: () => {},
    });
  }

  requestHoliday() {
    this.approvalService.requestHoliday().subscribe({
      next: () => {
        this.toastService.success('Holiday Requested Successfully');
      },
      error: () => {},
    });
  }
}
