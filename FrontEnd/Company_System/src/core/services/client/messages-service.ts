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
  private readonly _messagesApiService = inject(MessagesApiService);
  private readonly _toastService = inject(ToastService);
  private readonly _messageHubService = inject(MessagesHubService);
  private readonly _chatPanelService = inject(ChatPanelService);
  private readonly _authService = inject(AuthService);

  // signals
  private _isLoading = signal<boolean>(false);

  // private
  private _messages = signal<Record<string, MessageDTO[]>>({});
  private _usersWithNoMoreMessages = new Set<string>();
  private _takenMessagesPerUser: Record<string, number> = {};
  private _sectionSize = 10;

  // computed
  readonly messages = computed(() => {
    const currUserId = this._authService.userData()?.userId;
    const otherUserId = this._chatPanelService.node()?.userId;

    if (currUserId == null || otherUserId == null) return [];

    return this._messages()[this.generateGroupId(currUserId, otherUserId)];
  });

  // subject
  readonly cancelRequests$ = new Subject<void>();

  // getters

  get isLoading() {
    return this._isLoading.asReadonly();
  }

  // constructor
  constructor() {
    this._messageHubService.onMessageReceived$.subscribe({
      next: (newMessage) => {
        this._messages.update((curr) => ({
          ...curr,
          [newMessage.groupName]: [...(curr[newMessage.groupName] ?? []), newMessage].sort(
            (a, b) => (a.createdAt < b.createdAt ? -1 : 1),
          ),
        }));

        this._takenMessagesPerUser[newMessage.groupName] =
          (this._takenMessagesPerUser[newMessage.groupName] ?? 0) + 1;
      },
    });
  }

  // methods

  reset() {
    this.cancelRequests$.next();

    this._messages.set({});
    this._isLoading.set(false);
    this._usersWithNoMoreMessages = new Set();
    this._takenMessagesPerUser = {};
  }

  generateGroupId(currUserId: string, otherUserId: string) {
    if (currUserId > otherUserId) return `${currUserId}-${otherUserId}`;

    return `${otherUserId}-${currUserId}`;
  }

  // lazy load data
  loadMoreMessages(otherUserId: string) {
    if (this._isLoading() || this._usersWithNoMoreMessages.has(otherUserId) || !otherUserId) return;
    this._isLoading.set(true);

    const groupId = this.generateGroupId(this._authService.userData()!.userId, otherUserId);
    const lazyData: LazyDTO = {
      taken: this._takenMessagesPerUser[groupId] ?? 0,
      sectionSize: this._sectionSize,
    };

    this._messagesApiService
      .lazyGetMessages(lazyData, otherUserId)
      .pipe(takeUntil(this.cancelRequests$))
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
            this._usersWithNoMoreMessages.add(otherUserId);
          }

          this._takenMessagesPerUser[groupId] =
            (this._takenMessagesPerUser[groupId] ?? 0) + data.length;

          this._isLoading.set(false);
        },
        error: () => {
          this._toastService.error('something went wrong while fetching messages');
          this._isLoading.set(false);
        },
      });
  }
}
