import { DragPanDirective } from '../../shared/directives/drag-pan.directive';
import {
  afterNextRender,
  Component,
  DestroyRef,
  ElementRef,
  inject,
  OnDestroy,
  OnInit,
  viewChild,
} from '@angular/core';
import { OrgNodeComponent } from './component/org-node/org-node.component';
import { OrgTreeService } from '../../core/services/api-services/org-tree-service';
import { DelegateTaskComponent } from './component/delegate-task/delegate-task.component';
import { DelegateTaskService } from './component/delegate-task/delegate-task.service';
import { LoadingComponent } from '../../shared/components/loading/loading.component';
import { IsVisableDirective } from '../../shared/directives/is-visable.directive';
import { ChatPanelComponent } from './component/chat-panel/chat-panel.component';
import { ChatPanelService } from './component/chat-panel/chat-panel.service';

@Component({
  selector: 'app-org-tree',
  standalone: true,
  imports: [
    OrgNodeComponent,
    DragPanDirective,
    DelegateTaskComponent,
    LoadingComponent,
    IsVisableDirective,
    ChatPanelComponent,
  ],
  providers: [DelegateTaskService],
  templateUrl: './org-tree.component.html',
})
export class OrgTreeComponent implements OnInit, OnDestroy {
  // DI
  protected readonly orgTreeService = inject(OrgTreeService);
  protected readonly delegateTaskService = inject(DelegateTaskService);
  protected readonly chatPanelService = inject(ChatPanelService);

  private readonly destroyRef = inject(DestroyRef);
  private readonly viewport = viewChild.required<ElementRef<HTMLElement>>('treeViewport');
  private readonly content = viewChild.required<ElementRef<HTMLElement>>('treeContent');

  constructor() {
    afterNextRender(() => {
      const viewport = this.viewport().nativeElement;
      let previousRange: number | null = null;

      const observer = new ResizeObserver(() => {
        const range = Math.max(0, viewport.scrollWidth - viewport.clientWidth);
        viewport.scrollLeft =
          previousRange === null ? range / 2 : viewport.scrollLeft + (range - previousRange) / 2;
        previousRange = range;
      });

      observer.observe(viewport);
      observer.observe(this.content().nativeElement);
      this.destroyRef.onDestroy(() => observer.disconnect());
    });
  }

  ngOnInit(): void {
    this.loadMoreTree();
  }

  // on destroy
  ngOnDestroy(): void {
    this.orgTreeService.cancelTreeRequests$.next();
  }

  loadMoreTree() {
    this.orgTreeService.loadMoreTree().subscribe({
      error: () => {},
    });
  }
}
