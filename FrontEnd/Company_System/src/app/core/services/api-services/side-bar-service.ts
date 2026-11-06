import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SideBarService {
  // signals
  private _isSideBarOpen = signal<boolean>(window.innerWidth >= 1024);

  // getters
  get isSideBarOpen() {
    return this._isSideBarOpen.asReadonly();
  }

  // methods

  reset() {
    this._isSideBarOpen.set(window.innerWidth >= 1024);
  }

  toggleSideBar() {
    this._isSideBarOpen.update((curr) => !curr);
  }

  close() {
    this._isSideBarOpen.set(false);
  }
}
