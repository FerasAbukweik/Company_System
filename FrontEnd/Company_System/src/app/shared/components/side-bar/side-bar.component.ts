import { Component, computed, DestroyRef, inject, OnInit } from '@angular/core';
import { ISideBarItem } from '../../../core/interfaces/side-bar-model';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { SideBarService } from '../../../core/services/api-services/side-bar-service';
import { IconComponent } from '../icon/icon.component';
import { HeroIcon } from '../../../core/constants/hero-icon-d';
import { AuthService } from '../../../core/services/api-services/auth-service';

@Component({
  selector: 'aside[app-side-bar]',
  imports: [RouterLink, RouterLinkActive, IconComponent],
  templateUrl: './side-bar.component.html',
  host: {
    '[class]': 'hostClass()',
    '[style.visibility]': "sideBarService.isSideBarOpen() ? 'visible' : 'hidden'",
    '[attr.aria-hidden]': '!sideBarService.isSideBarOpen()',
    '[attr.inert]': "sideBarService.isSideBarOpen() ? null : ''",
    '(click)': '$event.stopPropagation()',
  },
})
export class SideBarComponent implements OnInit {
  // DI
  protected readonly sideBarService = inject(SideBarService);
  private readonly _authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit() {
    this._authService
      .updateIsAdmin()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => {} });
  }

  private readonly isAdminRoute = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map(() => this.hasAdminRouteData()),
      startWith(this.hasAdminRouteData()),
    ),
  );

  protected readonly sideBarItems = computed<ISideBarItem[]>(() => {
    if (this._authService.isAdmin() && this.isAdminRoute()) {
      return [
        { label: 'Employees', icon: HeroIcon.users, route: '/admin/employees' },
        { label: 'Payments', icon: HeroIcon.banknotes, route: '/admin/payments' },
        { label: 'Attendance', icon: HeroIcon.pending, route: '/admin/attendance' },
        { label: 'Employee Pages', icon: HeroIcon.home, route: '/dashboard' },
      ];
    }

    const items: ISideBarItem[] = [
      { icon: HeroIcon.home, label: 'Dashboard', route: '/dashboard' },
      { icon: HeroIcon.chartLine, label: 'Org Tree', route: '/org-tree' },
      { icon: HeroIcon.banknotes, label: 'My Portal', route: '/my-portal' },
    ];

    if (this._authService.isAdmin()) {
      items.push({ label: 'Admin', icon: HeroIcon.users, route: '/admin' });
    }

    return items;
  });

  private hasAdminRouteData(): boolean {
    let route = this.router.routerState.snapshot.root;
    while (route) {
      if (route.data['isAdmin'] === true) return true;
      if (!route.firstChild) break;
      route = route.firstChild;
    }
    return false;
  }

  // computed
  hostClass = computed(() => {
    let result =
      'max-lg:absolute max-lg:h-full min-h-screen transition-all duration-300 min-w-[300px] overflow-hidden bg-surface-lowest border-r border-outline-light shadow-sm flex flex-col py-6 px-4 z-50';

    if (!this.sideBarService.isSideBarOpen()) {
      result += ' min-w-0! w-0! px-0! border-r-0';
    }

    return result;
  });

  hanleOnLinkClick() {
    if (window.innerWidth < 1024) {
      this.sideBarService.close();
    }
  }
}
