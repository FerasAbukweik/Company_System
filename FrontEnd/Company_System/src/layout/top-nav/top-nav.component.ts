import { Component, inject, signal } from '@angular/core';
import { AuthService } from '../../core/services/api/Auth-api-service';
import { Router } from '@angular/router';
import { TasksService } from '../../core/services/client/tasks-service';
import { ApprovalService } from '../../core/services/client/approval-service';
import { ActivitiesService } from '../../core/services/client/activities-service';

@Component({
  selector: 'nav[app-top-nav]',
  imports: [],
  templateUrl: './top-nav.component.html',
  host: {
    class:
      'sticky top-0 w-full h-16 bg-surface-base border-b border-outline-light flex justify-between items-center px-8 z-40',
  },
})
export class TopNavComponent {
  // DI
  protected readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly tasksService = inject(TasksService);
  private readonly approvalsService = inject(ApprovalService);
  private readonly activitiesService = inject(ActivitiesService);

  // signals
  showUserPanel = signal<boolean>(false);

  // methods

  // logout
  logout() {
    // send request to the server to remove tokens from http only cookies
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigateByUrl('/login');
      },
    });

    this.approvalsService.reset();
    this.tasksService.reset();
    this.activitiesService.reset();
  }

  // toggle show panel
  togglePanel() {
    this.showUserPanel.update((curr) => !curr);
  }
}
