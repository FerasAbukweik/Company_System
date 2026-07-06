import { Component, inject } from '@angular/core';
import { TopNavComponent } from "../top-nav/top-nav.component";
import { SideBarComponent } from "../side-bar/side-bar.component";
import { RouterOutlet } from '@angular/router';
import { ISideBarItem } from '../../core/interfaces/side-bar-model';
import { SideBarService } from '../../core/services/client/side-bar-service';

@Component({
  selector: 'app-main-layout-component',
  imports: [TopNavComponent, SideBarComponent, RouterOutlet],
  templateUrl: './main-layout-component.html',
})
export class MainLayoutComponent {
  // DI
  protected readonly sideBarService = inject(SideBarService);

  // protected
  protected sideBarItems: ISideBarItem[] = [
    { active: true, icon: 'dashboard', label: 'Dashboard', route: '/dashboard' },
    { active: false, icon: 'account_tree', label: 'Org Tree', route: '/org-tree' },
  ]
}
