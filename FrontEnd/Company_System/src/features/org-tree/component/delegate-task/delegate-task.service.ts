import { Injectable, signal } from '@angular/core';
import { OrgNodeDTO } from '../../../../core/dto/org-node';

@Injectable()
export class DelegateTaskService {
  // signals
  private _isShowDelegateTask = signal<boolean>(false);
  private _node = signal<OrgNodeDTO | null>(null);

  // getters
  get isShowDelegateTask() {
    return this._isShowDelegateTask.asReadonly();
  }

  get node() {
    return this._node.asReadonly();
  }

  // mehtods

  toggle() {
    this._isShowDelegateTask.update((curr) => !curr);
  }

  close() {
    this._isShowDelegateTask.set(false);
    this._node.set(null);
  }

  open(node: OrgNodeDTO) {
    this._isShowDelegateTask.set(true);
    this._node.set(node);
  }
}
