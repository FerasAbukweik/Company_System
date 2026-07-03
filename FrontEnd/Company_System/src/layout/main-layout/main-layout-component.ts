import { Component } from '@angular/core';
import { TopNavComponent } from "../top-nav/top-nav.component";
import { SideBarComponent } from "../side-bar/side-bar.component";
import { RouterOutlet } from '@angular/router';
import { ISideBarItem } from '../../core/interfaces/side-bar-model';

@Component({
  selector: 'app-main-layout-component',
  imports: [TopNavComponent, SideBarComponent, RouterOutlet],
  templateUrl: './main-layout-component.html',
})
export class MainLayoutComponent {
  protected sideBarItems: ISideBarItem[] = [
    { active: true, icon: 'dashboard', label: 'Dashboard', route: '/dashboard' },
    { active: false, icon: 'account_tree', label: 'Org Tree', route: '/org-tree' },
    { active: false, icon: 'assignment', label: 'Task Manager', route: '/task-manager' },
    { active: false, icon: 'chat', label: 'Chat', route: '/chat' }
  ]
}
