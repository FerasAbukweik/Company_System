import { MessagesService } from '../../core/services/api-services/messages-service';
import { OrgTreeService } from '../../core/services/api-services/org-tree-service';
import { ApprovalService } from '../../core/services/api-services/approval-service';
import { ActivitiesService } from '../../core/services/api-services/activities-service';
import { TasksService } from '../../core/services/api-services/tasks-service';
import { Component, computed, inject, OnDestroy } from '@angular/core';
import { TopNavComponent } from '../../shared/components/top-nav/top-nav.component';
import { SideBarComponent } from '../../shared/components/side-bar/side-bar.component';
import { RouterOutlet } from '@angular/router';
import { SideBarService } from '../../core/services/api-services/side-bar-service';
import { AuthService } from '../../core/services/api-services/auth-service';
import { HeroIcon } from '../../core/constants/hero-icon-d';
import { IconComponent } from '../../shared/components/icon/icon.component';

@Component({
  selector: 'app-main-layout-component',
  imports: [TopNavComponent, SideBarComponent, RouterOutlet, IconComponent],
  templateUrl: './main-layout-component.html',
})
export class MainLayoutComponent implements OnDestroy {
  protected readonly HeroIcon = HeroIcon;

  // DI
  protected readonly sideBarService = inject(SideBarService);
  private readonly _authService = inject(AuthService);

  private readonly tasksService = inject(TasksService);

  private readonly activitiesService = inject(ActivitiesService);

  private readonly approvalService = inject(ApprovalService);

  private readonly orgTreeService = inject(OrgTreeService);

  private readonly messagesService = inject(MessagesService);

  ngOnDestroy() {
    this.tasksService.reset();
    this.activitiesService.reset();
    this.approvalService.resetToApprova();
    this.approvalService.resetRequested();
    this.orgTreeService.reset();
    this.messagesService.reset();
  }

  // getters
  get isScreenSmall() {
    return window.innerWidth < 1024;
  }
}
