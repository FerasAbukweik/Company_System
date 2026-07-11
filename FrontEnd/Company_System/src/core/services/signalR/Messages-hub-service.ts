import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { Urls } from '../../constants/urls';
import { MessageDTO } from '../../dto/message-dto';
import { ToastService } from '../client/toast-service';

@Injectable({
  providedIn: 'root',
})
export class MessagesHubService {
  // DI
  private readonly toastService = inject(ToastService);

  // Hub connection
  private connection: signalR.HubConnection | null = null;

  // Observables
  readonly onTyping$ = new Subject<void>();
  readonly onStopTyping$ = new Subject<void>();
  readonly onMessageReceived$ = new Subject<MessageDTO>();

  async startConnection(otherUserId: string): Promise<void> {
    // Stop any existing connection
    if (this.connection) {
      await this.stopConnection();
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${Urls.messagesHub}?userId=${otherUserId}`, {
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .build();

    this.registerHandlers();

    try {
      await this.connection.start();
    } catch (err) {
      this.toastService.error('Failed connecting to messages hub');
    }
  }

  async stopConnection(): Promise<void> {
    if (!this.connection) return;

    try {
      await this.connection.stop();
      this.connection = null;
    } catch (err) {
      this.toastService.error('failed disconnecting from messages hub');
    }
  }

  private registerHandlers(): void {
    if (!this.connection) {
      return;
    }

    this.connection.on('NotifyTyping', () => {
      this.onTyping$.next();
    });

    this.connection.on('NotifyStoppedTyping', () => {
      this.onStopTyping$.next();
    });

    this.connection.on('ReceiveMessage', (message: MessageDTO) => {
      this.onMessageReceived$.next(message);
    });
  }

  async sendMessage(content: string): Promise<void> {
    if (!this.isConnected()) return;

    await this.connection!.invoke('SendMessage', content);
  }

  async notifyTyping(): Promise<void> {
    if (!this.isConnected()) return;

    await this.connection!.invoke('NotifyTyping');
  }

  async notifyStoppedTyping(): Promise<void> {
    if (!this.isConnected()) return;

    await this.connection!.invoke('NotifyStoppedTyping');
  }

  private isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}
