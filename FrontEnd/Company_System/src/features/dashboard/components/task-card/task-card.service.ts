import { Injectable, signal } from '@angular/core';

@Injectable()
export class TaskCardService {
  // DI

  // signals
  private _showActivityStatusMenuFor = signal<string>('');

  // getters
  get isStatusMenuShown() {
    return !!this._showActivityStatusMenuFor();
  }

  // methods

  toggleShowActivityStatusMenu(showForId: string) {
    this._showActivityStatusMenuFor.update((curr) => (!!curr ? '' : showForId));
  }

  closeStatusMenu() {
    this._showActivityStatusMenuFor.set('');
  }

  IsStatusMenuShownFor(taskId: string) {
    return this._showActivityStatusMenuFor() === taskId;
  }
}
