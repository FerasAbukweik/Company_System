import { Injectable, signal } from '@angular/core';

@Injectable()
export class TaskCardService {
  // DI

  // signals
  private showActivityStatusMenuFor = signal<string>('');

  // getters
  get getIsStatusMenuShown() {
    return !!this.showActivityStatusMenuFor();
  }

  // methods

  toggleShowActivityStatusMenu(showForId: string) {
    this.showActivityStatusMenuFor.update((curr) => (!!curr ? '' : showForId));
  }

  closeStatusMenu() {
    this.showActivityStatusMenuFor.set('');
  }

  IsStatusMenuShownFor(taskId: string) {
    return this.showActivityStatusMenuFor() === taskId;
  }
}
