import { Component, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrgNodeDTO } from '../../../../core/dto/org-node';
import { PositionsEnum } from '../../../../core/enum/positions-enum';
import { DelegateTaskService } from '../delegate-task/delegate-task.service';
import { ChatPanelService } from '../chat-panel/chat-panel.service';

@Component({
  selector: 'app-org-node',
  standalone: true,
  templateUrl: './org-node.component.html',
  imports: [CommonModule, OrgNodeComponent],
})
export class OrgNodeComponent {
  // DI
  protected readonly delegateTaskService = inject(DelegateTaskService);
  protected readonly chatPanelService = inject(ChatPanelService);

  // input
  node = input.required<OrgNodeDTO>();
  isCurrUserChild = input.required<boolean>();

  // getters
  get displayRole(): string {
    return PositionsEnum[this.node().position] || 'Unknown';
  }
}
