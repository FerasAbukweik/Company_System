import { Component, inject, OnInit } from '@angular/core';
import { OrgNodeComponent } from './component/org-node/org-node.component';
import { OrgTreeService } from '../../core/services/client/org-tree-service';
import { DelegateTaskComponent } from './component/delegate-task/delegate-task.component';
import { DelegateTaskService } from './component/delegate-task/delegate-task.service';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { IsVisableDirective } from '../../shared/directives/is-visable.directive';
import { ChatPanelComponent } from './component/chat-panel/chat-panel.component';

@Component({
  selector: 'app-org-tree',
  standalone: true,
  imports: [
    OrgNodeComponent,
    DelegateTaskComponent,
    LoadingComponent,
    IsVisableDirective,
    ChatPanelComponent,
  ],
  providers: [DelegateTaskService],
  templateUrl: './org-tree.component.html',
})
export class OrgTreeComponent implements OnInit {
  // DI
  protected readonly orgTreeService = inject(OrgTreeService);
  protected readonly delegateTaskService = inject(DelegateTaskService);

  ngOnInit(): void {
    this.orgTreeService.loadMoreTree();
  }
}
