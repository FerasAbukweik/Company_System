import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { defer, Subject } from 'rxjs';
import { toastError } from '../../utils/toast-error';
import { Urls } from '../../constants/urls';
import { MessageDTO } from '../../dto/message/message-dto';
import { ToastService } from '../api-services/toast-service';

@Injectable({
  providedIn: 'root',
})
export class MessagesHubService {
  // DI
  private readonly toastService = inject(ToastService);

  // Hub connection
  private connection: signalR.HubConnection | null = null;
  private connectionVersion = 0;

  // Observables
  readonly onTyping$ = new Subject<void>();
  readonly onStopTyping$ = new Subject<void>();
  readonly onMessageReceived$ = new Subject<MessageDTO>();

  async startConnection(otherUserId: string): Promise<void> {
    const version = ++this.connectionVersion;
    const previous = this.connection;
    this.connection = null;
    if (previous) {
      try {
        await previous.stop();
      } catch {
        this.toastService.error('Failed disconnecting from messages hub');
      }
    }

    if (version !== this.connectionVersion) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${Urls.messagesHub}?userId=${otherUserId}`, {
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .build();

    const connection = this.connection;
    this.registerHandlers();

    try {
      await connection.start();
    } catch {
      if (version === this.connectionVersion) {
        this.toastService.error('Failed connecting to messages hub');
      }
    }

    if (version !== this.connectionVersion) {
      try {
        await connection.stop();
      } catch {
        // A replaced connection must not affect the active conversation.
      }
    }
  }

  async stopConnection(): Promise<void> {
    ++this.connectionVersion;
    const connection = this.connection;
    this.connection = null;
    if (!connection) return;

    try {
      await connection.stop();
    } catch (err) {
      this.toastService.error('failed disconnecting from messages hub');
    }
  }

  private registerHandlers(): void {
    if (!this.connection) {
      return;
    }

    const connection = this.connection;
    connection.on('NotifyTyping', () => {
      if (this.connection !== connection) return;
      this.onTyping$.next();
    });

    connection.on('NotifyStoppedTyping', () => {
      if (this.connection !== connection) return;
      this.onStopTyping$.next();
    });

    connection.on('ReceiveMessage', (message: MessageDTO) => {
      if (this.connection !== connection) return;
      this.onMessageReceived$.next(message);
    });
  }

  sendMessage(content: string) {
    return defer(async () => {
      if (!this.isConnected()) throw new Error('Messages hub is not connected');
      await this.connection!.invoke('SendMessage', content);
    }).pipe(toastError(this.toastService, 'تعذر إرسال الرسالة'));
  }

  async notifyTyping(): Promise<void> {
    if (!this.isConnected()) return;

    try {
      await this.connection!.invoke('NotifyTyping');
    } catch {
      // Typing notifications are transient; a failed one must not reject into the UI.
    }
  }

  async notifyStoppedTyping(): Promise<void> {
    if (!this.isConnected()) return;

    try {
      await this.connection!.invoke('NotifyStoppedTyping');
    } catch {
      // A disconnected client cannot notify the other user.
    }
  }

  private isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}
