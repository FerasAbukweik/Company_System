import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SideBarService {
  // signals
  private isSideBarOpen = signal<boolean>(window.innerWidth >= 1024);

  // getters
  get getIsSideBarOpen() {
    return this.isSideBarOpen.asReadonly();
  }

  // methods
  toggleSideBar() {
    this.isSideBarOpen.update((curr) => !curr);
  }

  close() {
    this.isSideBarOpen.set(false);
  }
}
