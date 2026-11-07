import { Injectable, inject } from '@angular/core';
import { Subject } from 'rxjs';
import { NotificationDTO } from '../../dto/notification/notification-dto';
import { ToastService } from '../api-services/toast-service';
import { SignalRService } from './signalr-service';

@Injectable({ providedIn: 'root' })
export class NotificationHubService {
  private readonly signalR = inject(SignalRService);
  private readonly toast = inject(ToastService);
  readonly onNotificationReceived$ = new Subject<NotificationDTO>();
  readonly onReconnected$ = this.signalR.onReconnected$.asObservable();

  constructor() {
    this.signalR.connection.on('ReceiveNotification', (notification: NotificationDTO) =>
      this.onNotificationReceived$.next(notification),
    );
    this.signalR.onConnectionLost$.subscribe(() =>
      this.toast.error('Live updates disconnected. You can retry from the notification menu.'),
    );
  }

  async startConnection() {
    await this.signalR.start();
  }
}
