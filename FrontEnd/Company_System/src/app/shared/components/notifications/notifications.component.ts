import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { IsVisibleDirective } from '../../directives/is-visible.directive';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { NotificationsService } from '../../../core/services/api-services/notifications-service';
import { NotificationDTO } from '../../../core/dto/notification/notification-dto';
import { ToastService } from '../../../core/services/api-services/toast-service';
import { HeroIcon } from '../../../core/constants/hero-icon-d';
import { IconComponent } from '../icon/icon.component';

@Component({
  selector: 'app-notifications',
  imports: [DatePipe, IconComponent, IsVisibleDirective],
  templateUrl: './notifications.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'relative inline-flex' },
})
export class NotificationsComponent {
  protected readonly service = inject(NotificationsService);
  protected readonly HeroIcon = HeroIcon;
  protected readonly isOpen = signal(false);
  protected readonly listVisible = signal(false);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  constructor() {
    effect(() => {
      const open = this.isOpen();
      const visible = this.listVisible();
      const unreadCount = this.service.unreadCount();

      untracked(() => {
        if (!open) this.listVisible.set(false);
        else if (visible && unreadCount > 0) this.markAllRead();
      });
    });
  }

  protected toggle() {
    this.isOpen.update((open) => !open);
    if (this.isOpen() && this.service.notifications().length === 0) this.service.loadMore();
  }

  protected open(notification: NotificationDTO) {
    this.service.markAllRead().subscribe({
      next: () => {
        this.isOpen.set(false);
        void this.router.navigateByUrl(notification.targetPath);
      },
      error: () => this.toast.error('Unable to mark notifications as read'),
    });
  }

  protected markAllRead() {
    this.service
      .markAllRead()
      .subscribe({ error: () => this.toast.error('Unable to mark notifications as read') });
  }
}
