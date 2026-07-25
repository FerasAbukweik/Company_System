import { inject, Injectable, signal } from '@angular/core';
import { OrgNodeDTO } from '../../../../core/dto/org-node';
import { MessagesHubService } from '../../../../core/services/signalR/Messages-hub-service';

@Injectable({ providedIn: 'root' })
export class ChatPanelService {
  // DI
  private readonly _messageHubService = inject(MessagesHubService);

  // siganls
  private _isVisable = signal<boolean>(false);
  private _node = signal<OrgNodeDTO | null>(null);

  // getters
  get isVisable() {
    return this._isVisable.asReadonly();
  }

  get node() {
    return this._node.asReadonly();
  }

  // methods

  close() {
    this._isVisable.set(false);
    this._node.set(null);
    this._messageHubService.stopConnection();
  }

  open(node: OrgNodeDTO) {
    this._isVisable.set(true);
    this._node.set(node);
    this._messageHubService.startConnection(node.userId);
  }
}
