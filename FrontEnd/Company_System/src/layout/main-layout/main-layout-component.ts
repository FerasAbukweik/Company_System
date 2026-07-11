import { Component, computed, inject } from '@angular/core';
import { TopNavComponent } from '../top-nav/top-nav.component';
import { SideBarComponent } from '../side-bar/side-bar.component';
import { RouterOutlet } from '@angular/router';
import { SideBarService } from '../../core/services/client/side-bar-service';
import { AuthService } from '../../core/services/client/auth-service';
import { HeroIcon } from '../../core/constants/hero-icon-d';

@Component({
  selector: 'app-main-layout-component',
  imports: [TopNavComponent, SideBarComponent, RouterOutlet],
  templateUrl: './main-layout-component.html',
})
export class MainLayoutComponent {
  // DI
  protected readonly sideBarService = inject(SideBarService);
  private readonly authService = inject(AuthService);

  // protected
  protected sideBarItems = computed(() => {
    let result = [
      { icon: HeroIcon.home, label: 'Dashboard', route: '/dashboard' },
      { icon: HeroIcon.chartLine, label: 'Org Tree', route: '/org-tree' },
    ];

    if (this.authService.getIsAdmin()) {
      result = [
        ...result,
        {
          label: 'Add Employee',
          icon: HeroIcon.add,
          route: 'add-employee',
        },
      ];
    }

    return result;
  });

  // getters
  get isScreenSmall() {
    return window.innerWidth < 1024;
  }
}
