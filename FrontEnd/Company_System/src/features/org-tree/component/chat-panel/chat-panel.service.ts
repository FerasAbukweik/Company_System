import { inject, Injectable, signal } from '@angular/core';
import { OrgNodeDTO } from '../../../../core/dto/org-node';
import { MessagesHubService } from '../../../../core/services/signalR/Messages-hub-service';

@Injectable({ providedIn: 'root' })
export class ChatPanelService {
  // DI
  private readonly messageHubService = inject(MessagesHubService);

  // siganls
  private isVisable = signal<boolean>(false);
  private node = signal<OrgNodeDTO | null>(null);

  // getters
  get getIsVisable() {
    return this.isVisable.asReadonly();
  }

  get getNode() {
    return this.node.asReadonly();
  }

  // methods

  close() {
    this.isVisable.set(false);
    this.node.set(null);
    this.messageHubService.stopConnection();
  }

  open(node: OrgNodeDTO) {
    this.isVisable.set(true);
    this.node.set(node);
    this.messageHubService.startConnection(node.userId);
  }
}
