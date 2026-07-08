import { Component, inject, signal } from '@angular/core';
import { AuthService } from '../../core/services/client/auth-service';

@Component({
  selector: 'nav[app-top-nav]',
  imports: [],
  templateUrl: './top-nav.component.html',
  host: {
    class:
      'sticky top-0 w-full h-16 bg-surface-base border-b border-outline-light flex justify-between items-center px-8 z-40',
  },
})
export class TopNavComponent {
  // DI
  protected readonly authService = inject(AuthService);

  // signals
  showUserMenu = signal<boolean>(false);

  // methods

  // logout
  logout() {
    this.authService.logout();
  }

  // toggle show panel
  togglePanel() {
    this.showUserMenu.update((curr) => !curr);
  }
}
