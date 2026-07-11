import { Injectable, signal } from '@angular/core';
import { OrgNodeDTO } from '../../../../core/dto/org-node';

@Injectable()
export class DelegateTaskService {
  // signals
  private isShowDelegateTask = signal<boolean>(false);
  private node = signal<OrgNodeDTO | null>(null);

  // getters
  get getIsShowDelegateTask() {
    return this.isShowDelegateTask.asReadonly();
  }

  get getNode() {
    return this.node.asReadonly();
  }

  // mehtods

  toggle() {
    this.isShowDelegateTask.update((curr) => !curr);
  }

  close() {
    this.isShowDelegateTask.set(false);
    this.node.set(null);
  }

  open(node: OrgNodeDTO) {
    this.isShowDelegateTask.set(true);
    this.node.set(node);
  }
}
