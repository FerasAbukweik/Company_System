import { Injectable, inject, signal } from '@angular/core';
import { defer, finalize, Observable, of, shareReplay, tap } from 'rxjs';
import { NotificationsApiService } from '../api/notifications-api-service';
import { NotificationHubService } from '../signalR/notification-hub-service';
import { SignalRService } from '../signalR/signalr-service';
import { NotificationDTO } from '../../dto/notification/notification-dto';
import { ToastService } from './toast-service';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly api = inject(NotificationsApiService);
  private readonly hub = inject(NotificationHubService);
  private readonly signalR = inject(SignalRService);
  private readonly toast = inject(ToastService);

  private version = 0;
  private markingAllRead: Observable<boolean> | null = null;

  readonly notifications = signal<NotificationDTO[]>([]);
  readonly unreadCount = signal(0);
  readonly isMoreAvaiable = signal(true);
  readonly loading = signal(false);
  readonly loadFailed = signal(false);
  readonly connectionState = this.signalR.state;
  readonly sectionSize = 10;

  constructor() {
    this.hub.onNotificationReceived$.subscribe((notification) => {
      if (this.notifications().some((item) => item.id === notification.id)) return;

      this.notifications.update((items) => [notification, ...items]);
      this.unreadCount.update((count) => count + 1);
    });

    this.hub.onReconnected$.subscribe(() => {
      // Events missed during a disconnect must be recovered from the server.
      this.clearList();
      this.refreshUnread();
      this.loadMore();
    });
  }

  start() {
    void this.hub.startConnection();
    this.refreshUnread();
  }

  retry() {
    void this.hub.startConnection();
  }

  loadMore() {
    if (this.loading() || !this.isMoreAvaiable()) return;

    const version = this.version;
    const taken = this.notifications().length;
    this.loadFailed.set(false);
    this.loading.set(true);

    this.api
      .get({ taken, sectionSize: this.sectionSize })
      .pipe(
        tap((response) => {
          if (version !== this.version) return;

          this.notifications.update((items) => {
            const existingIds = new Set(items.map((item) => item.id));
            const next = response.items.filter((item) => !existingIds.has(item.id));
            return [...items, ...next];
          });
          this.isMoreAvaiable.set(response.items.length >= this.sectionSize);
        }),
        finalize(() => {
          if (version === this.version) this.loading.set(false);
        }),
      )
      .subscribe({
        error: () => {
          if (version !== this.version) return;
          this.loadFailed.set(true);
          this.toast.error('Unable to load notifications');
        },
      });
  }

  markAllRead(): Observable<boolean> {
    return defer(() => {
      if (this.unreadCount() === 0) return of(true);
      if (this.markingAllRead) return this.markingAllRead;

      const version = this.version;
      const unreadCount = this.unreadCount();
      const receivedIds = new Set(this.notifications().map((item) => item.id));
      const request = this.api.markAllRead().pipe(
        tap(() => {
          if (version !== this.version) return;
          this.unreadCount.update((count) => Math.max(0, count - unreadCount));
          this.notifications.update((items) =>
            items.map((item) => (receivedIds.has(item.id) ? { ...item, isRead: true } : item)),
          );
        }),
        finalize(() => {
          if (this.markingAllRead === request) this.markingAllRead = null;
        }),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
      this.markingAllRead = request;
      return request;
    });
  }

  reset() {
    this.clearList();
    this.unreadCount.set(0);
  }

  private refreshUnread() {
    const version = this.version;
    const notifications = this.notifications();
    this.api.unreadCount().subscribe({
      next: (count) => {
        if (version !== this.version) return;
        if (notifications !== this.notifications()) {
          this.refreshUnread();
          return;
        }
        this.unreadCount.set(count);
      },
      error: () => this.toast.error('Unable to load notification count'),
    });
  }

  private clearList() {
    this.version++;
    this.markingAllRead = null;
    this.notifications.set([]);
    this.isMoreAvaiable.set(true);
    this.loading.set(false);
    this.loadFailed.set(false);
  }
}
