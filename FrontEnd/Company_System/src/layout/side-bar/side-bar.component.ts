import { Component, input } from '@angular/core';
import { ISideBarItem } from '../../core/interfaces/side-bar-model';

@Component({
  selector: 'aside[app-side-bar]',
  imports: [],
  templateUrl: './side-bar.component.html',
  host: {
    class: 'min-h-screen w-65 bg-surface-lowest border-r border-outline-light shadow-sm flex flex-col py-6 px-4'
  }
})
export class SideBarComponent {
  navItems = input.required<ISideBarItem[]>();
}
