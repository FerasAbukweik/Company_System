import { Injectable, inject } from '@angular/core';
import { HubConnectionState } from '@microsoft/signalr';
import { defer, Subject } from 'rxjs';
import { toastError } from '../../utils/toast-error';
import { MessageDTO } from '../../dto/message/message-dto';
import { ToastService } from '../api-services/toast-service';
import { AuthService } from '../api-services/auth-service';
import { SignalRService } from './signalr-service';

@Injectable({ providedIn: 'root' })
export class MessagesHubService {
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthService);
  private readonly signalR = inject(SignalRService);
  private readonly connection = this.signalR.connection;
  private currentUserId: string | null = null;
  private groupOperations: Promise<void> = Promise.resolve();

  readonly onTyping$ = new Subject<void>();
  readonly onStopTyping$ = new Subject<void>();
  readonly onMessageReceived$ = new Subject<MessageDTO>();
  readonly onReconnected$ = new Subject<void>();

  constructor() {
    this.connection.on('ReceiveMessage', (message: MessageDTO) => {
      this.onMessageReceived$.next({
        ...message,
        isCurrUserSender: message.senderId === this.auth.userData()?.userId,
      });
    });
    this.connection.on('NotifyTyping', (userId: string) => {
      if (userId === this.currentUserId) this.onTyping$.next();
    });
    this.connection.on('NotifyStoppedTyping', (userId: string) => {
      if (userId === this.currentUserId) this.onStopTyping$.next();
    });
    this.signalR.onReconnected$.subscribe(() => {
      this.groupOperations = this.groupOperations.then(async () => {
        const userId = this.currentUserId;
        if (userId) await this.addToGroup(userId);
        this.onReconnected$.next();
      });
    });
  }

  startConnection(otherUserId: string): Promise<void> {
    this.currentUserId = otherUserId;
    this.groupOperations = this.groupOperations.then(async () => {
      await this.signalR.start();
      if (this.currentUserId !== otherUserId) return;
      await this.addToGroup(otherUserId);
    });
    return this.groupOperations;
  }

  // Leaving a conversation never stops the session's shared connection.
  async stopConnection(): Promise<void> {
    const previous = this.currentUserId;
    this.currentUserId = null;
    this.groupOperations = this.groupOperations.then(async () => {
      if (!previous || this.connection.state !== HubConnectionState.Connected) return;
      try {
        await this.connection.invoke('RemoveFromGroup', previous);
      } catch {
        /* Reconnect clears groups. */
      }
    });
    await this.groupOperations;
  }

  sendMessage(content: string) {
    const recipient = this.currentUserId;
    return defer(async () => {
      await this.groupOperations;
      if (
        !recipient ||
        recipient !== this.currentUserId ||
        this.connection.state !== HubConnectionState.Connected
      )
        throw new Error('Messages hub is not connected');
      await this.connection.invoke('SendMessage', recipient, content);
    }).pipe(toastError(this.toast, 'تعذر إرسال الرسالة'));
  }

  async notifyTyping(): Promise<void> {
    if (!this.currentUserId || this.connection.state !== HubConnectionState.Connected) return;
    try {
      await this.connection.send('NotifyTyping', this.currentUserId);
    } catch {
      /* Transient typing event. */
    }
  }

  async notifyStoppedTyping(): Promise<void> {
    if (!this.currentUserId || this.connection.state !== HubConnectionState.Connected) return;
    try {
      await this.connection.send('NotifyStoppedTyping', this.currentUserId);
    } catch {
      /* Transient typing event. */
    }
  }

  private async addToGroup(userId: string) {
    if (this.connection.state !== HubConnectionState.Connected) return;
    try {
      await this.connection.invoke('AddToGroup', userId);
    } catch {
      this.toast.error('Unable to join the conversation');
    }
  }
}
