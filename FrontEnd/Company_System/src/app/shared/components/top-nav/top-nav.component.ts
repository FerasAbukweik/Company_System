import { NotificationsComponent } from '../notifications/notifications.component';
import { Component, inject, signal } from '@angular/core';
import { AuthService } from '../../../core/services/api-services/auth-service';
import { IconComponent } from '../icon/icon.component';
import { HeroIcon } from '../../../core/constants/hero-icon-d';

@Component({
  selector: 'nav[app-top-nav]',
  imports: [IconComponent, NotificationsComponent],
  templateUrl: './top-nav.component.html',
  host: {
    class:
      'sticky top-0 w-full h-16 bg-surface-base border-b border-outline-light flex justify-between items-center px-8 z-40',
  },
})
export class TopNavComponent {
  protected readonly HeroIcon = HeroIcon;

  // DI
  protected readonly authService = inject(AuthService);

  // signals
  showUserMenu = signal<boolean>(false);

  // methods

  // logout
  logout() {
    this.authService.logout().subscribe({
      error: () => {},
    });
  }

  // toggle show panel
  togglePanel() {
    this.showUserMenu.update((curr) => !curr);
  }
}
