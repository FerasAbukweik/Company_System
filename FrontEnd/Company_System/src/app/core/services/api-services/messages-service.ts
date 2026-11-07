import { ToastService } from './toast-service';
import { toastError } from '../../utils/toast-error';
import { computed, inject, Injectable, signal } from '@angular/core';
import { MessagesApiService } from '../api/messages-api-service';
import { MessageDTO } from '../../dto/message/message-dto';
import { MessagesHubService } from '../signalR/messages-hub-service';
import { EMPTY, Subject, defer, finalize, takeUntil, tap } from 'rxjs';
import { ChatPanelService } from '../../../features/org-tree/component/chat-panel/chat-panel.service';
import { AuthService } from './auth-service';

@Injectable({ providedIn: 'root' })
export class MessagesService {
  private readonly _toastService = inject(ToastService);

  // DI
  private readonly _messagesApiService = inject(MessagesApiService);
  private readonly _messageHubService = inject(MessagesHubService);
  private readonly _chatPanelService = inject(ChatPanelService);
  private readonly _authService = inject(AuthService);

  // signals
  private _isLoading = signal<boolean>(false);

  // private
  private _messages = signal<Record<string, MessageDTO[]>>({});
  private _usersWithNoMoreMessages = new Set<string>();
  readonly sectionSize = 10;

  // computed
  readonly messages = computed(() => {
    const currUserId = this._authService.userData()?.userId;
    const otherUserId = this._chatPanelService.node()?.userId;

    if (currUserId == null || otherUserId == null) return [];

    return this._messages()[this.generateGroupId(currUserId, otherUserId)] ?? [];
  });

  // subject
  readonly cancelRequests$ = new Subject<void>();

  // getters

  get isLoading() {
    return this._isLoading.asReadonly();
  }

  // constructor
  receiveMessages() {
    return this._messageHubService.onMessageReceived$.pipe(
      tap({
        next: (newMessage) => {
          this._messages.update((curr) => ({
            ...curr,
            [newMessage.groupName]: this.mergeMessages(curr[newMessage.groupName] ?? [], [
              newMessage,
            ]),
          }));
        },
      }),
    );
  }

  // methods

  reset() {
    this.cancelRequests$.next();

    this._messages.set({});
    this._isLoading.set(false);
    this._usersWithNoMoreMessages = new Set();
  }

  resetConversation(otherUserId: string) {
    this.cancelRequests$.next();
    const userId = this._authService.userData()?.userId;
    if (!userId) return;

    const groupId = this.generateGroupId(userId, otherUserId);
    this._messages.update((current) => {
      const conversations = { ...current };
      delete conversations[groupId];
      return conversations;
    });
    this._usersWithNoMoreMessages.delete(otherUserId);
  }

  private mergeMessages(current: MessageDTO[], incoming: MessageDTO[]) {
    const messages = new Map([...current, ...incoming].map((message) => [message.id, message]));
    return [...messages.values()].sort(
      (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
    );
  }

  generateGroupId(currUserId: string, otherUserId: string) {
    if (currUserId > otherUserId) return `${currUserId}-${otherUserId}`;

    return `${otherUserId}-${currUserId}`;
  }

  // lazy load data
  loadMoreMessages(otherUserId: string) {
    return defer(() => {
      const userId = this._authService.userData()?.userId;
      if (
        !userId ||
        this._isLoading() ||
        this._usersWithNoMoreMessages.has(otherUserId) ||
        !otherUserId
      )
        return EMPTY;
      this._isLoading.set(true);

      const groupId = this.generateGroupId(userId, otherUserId);
      const lazyData = {
        taken: this._messages()[groupId]?.length ?? 0,
        sectionSize: this.sectionSize,
      };

      return this._messagesApiService
        .lazyGetMessages(lazyData, otherUserId)
        .pipe(toastError(this._toastService, 'تعذر تحميل الرسائل'), takeUntil(this.cancelRequests$))
        .pipe(
          tap({
            next: (data) => {
              if (data.length) {
                this._messages.update((curr) => ({
                  ...curr,
                  [groupId]: this.mergeMessages(curr[groupId] ?? [], data),
                }));
              }

              if (data.length === 0) {
                this._usersWithNoMoreMessages.add(otherUserId);
              }
            },
          }),
          finalize(() => {
            this._isLoading.set(false);
          }),
        );
    });
  }
}
