import { Component, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrgNodeDTO } from '../../../../core/dto/organization-hierarchy/org-node';
import { PositionsEnum } from '../../../../core/enum/positions-enum';
import { DelegateTaskService } from '../delegate-task/delegate-task.service';
import { ChatPanelService } from '../chat-panel/chat-panel.service';
import { AuthService } from '../../../../core/services/api-services/auth-service';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { HeroIcon } from '../../../../core/constants/hero-icon-d';

@Component({
  selector: 'app-org-node',
  standalone: true,
  templateUrl: './org-node.component.html',
  styleUrl: './org-node.component.css',
  host: { class: 'block w-max shrink-0' },
  imports: [CommonModule, OrgNodeComponent, IconComponent],
})
export class OrgNodeComponent {
  protected readonly HeroIcon = HeroIcon;

  // DI
  protected readonly delegateTaskService = inject(DelegateTaskService);
  protected readonly chatPanelService = inject(ChatPanelService);
  protected readonly authService = inject(AuthService);

  // input
  node = input.required<OrgNodeDTO>();
  isCurrUserChild = input.required<boolean>();

  // getters
  get displayRole(): string {
    return PositionsEnum[this.node().position] || 'Unknown';
  }

  get isCurrUser() {
    return this.node().userId === (this.authService.userData()?.userId ?? '');
  }
}
