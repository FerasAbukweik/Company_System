import { Component, computed, inject, input } from '@angular/core';
import { ISideBarItem } from '../../core/interfaces/side-bar-model';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { SideBarService } from '../../core/services/client/side-bar-service';

@Component({
  selector: 'aside[app-side-bar]',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './side-bar.component.html',
  host: {
    '[class]': 'hostClass()',
    '(click)': '$event.stopPropagation()',
  },
})
export class SideBarComponent {
  // DI
  protected readonly sideBarService = inject(SideBarService);

  // inputs
  navItems = input.required<ISideBarItem[]>();

  // computed
  hostClass = computed(() => {
    let result =
      'max-md:absolute max-sm:h-full min-h-screen transition-all duration-300 min-w-[300px] overflow-hidden bg-surface-lowest border-r border-outline-light shadow-sm flex flex-col py-6 px-4 z-50';

    if (!this.sideBarService.isSideBarOpen()) {
      result += ' min-w-0! w-0! px-0! border-r-0';
    }

    return result;
  });
}
