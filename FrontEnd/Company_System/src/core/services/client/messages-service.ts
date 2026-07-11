import { computed, inject, Injectable, signal } from '@angular/core';
import { MessagesApiService } from '../api/messages-api-service';
import { MessageDTO } from '../../dto/message-dto';
import { LazyDTO } from '../../dto/lazy-dto';
import { ToastService } from './toast-service';
import { MessagesHubService } from '../signalR/Messages-hub-service';
import { Subject, takeUntil } from 'rxjs';
import { ChatPanelService } from '../../../features/org-tree/component/chat-panel/chat-panel.service';
import { AuthService } from './auth-service';

@Injectable({ providedIn: 'root' })
export class MessagesService {
  // DI
  private readonly messagesApiService = inject(MessagesApiService);
  private readonly toastService = inject(ToastService);
  private readonly messageHubService = inject(MessagesHubService);
  private readonly chatPanelService = inject(ChatPanelService);
  private readonly authService = inject(AuthService);

  // signals
  private isLoading = signal<boolean>(false);

  // private
  private _messages = signal<Record<string, MessageDTO[]>>({});
  private usersWithNoMoreMessages = new Set<string>();
  private takenMessagesPerUser: Record<string, number> = {};
  private sectionSize = 10;

  // computed
  readonly messages = computed(() => {
    const currUserId = this.authService.getUserData()?.userId;
    const otherUserId = this.chatPanelService.getNode()?.userId;

    if (currUserId == null || otherUserId == null) return [];

    return this._messages()[this.generateGroupId(currUserId, otherUserId)];
  });
  // subject
  private stopRequests$ = new Subject<void>();

  // getters

  get getIsLoading() {
    return this.isLoading.asReadonly();
  }

  // constructor
  constructor() {
    this.messageHubService.onMessageReceived$.subscribe({
      next: (newMessage) => {
        this._messages.update((curr) => ({
          ...curr,
          [newMessage.groupName]: [...(curr[newMessage.groupName] ?? []), newMessage].sort(
            (a, b) => (a.createdAt < b.createdAt ? -1 : 1),
          ),
        }));

        this.takenMessagesPerUser[newMessage.groupName] =
          (this.takenMessagesPerUser[newMessage.groupName] ?? 0) + 1;
      },
    });
  }

  // methods

  reset() {
    this.stopRequests$.next();

    this._messages.set({});
    this.isLoading.set(false);
    this.usersWithNoMoreMessages = new Set();
    this.takenMessagesPerUser = {};
  }

  generateGroupId(currUserId: string, otherUserId: string) {
    if (currUserId > otherUserId) return `${currUserId}-${otherUserId}`;

    return `${otherUserId}-${currUserId}`;
  }

  // lazy load data
  loadMoreMessages(otherUserId: string) {
    if (this.isLoading() || this.usersWithNoMoreMessages.has(otherUserId) || !otherUserId) return;
    this.isLoading.set(true);

    const groupId = this.generateGroupId(this.authService.getUserData()!.userId, otherUserId);
    const lazyData: LazyDTO = {
      taken: this.takenMessagesPerUser[groupId] ?? 0,
      sectionSize: this.sectionSize,
    };

    this.messagesApiService
      .lazyGetMessages(lazyData, otherUserId)
      .pipe(takeUntil(this.stopRequests$))
      .subscribe({
        next: (data) => {
          if (data.length) {
            this._messages.update((curr) => ({
              ...curr,
              [data[0].groupName]: [...(curr[data[0].groupName] ?? []), ...data].sort((a, b) =>
                a.createdAt < b.createdAt ? -1 : 1,
              ),
            }));
          }

          if (data.length === 0) {
            this.usersWithNoMoreMessages.add(otherUserId);
          }

          this.takenMessagesPerUser[groupId] =
            (this.takenMessagesPerUser[groupId] ?? 0) + data.length;

          this.isLoading.set(false);
        },
        error: () => {
          this.toastService.error('something went wrong while fetching messages');
          this.isLoading.set(false);
        },
      });
  }
}
