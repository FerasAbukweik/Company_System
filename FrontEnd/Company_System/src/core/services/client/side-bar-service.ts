import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SideBarService {
  // signals
  isSideBarOpen = signal<boolean>(window.innerWidth >= 1024);


  // methods
  toggleSideBar() {
    this.isSideBarOpen.update((curr) => !curr);
  }
}
