import { catchError, EMPTY, finalize, switchMap } from 'rxjs';
import {
  Component,
  signal,
  inject,
  DestroyRef,
  OnInit,
  viewChild,
  ElementRef,
  OnDestroy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatPanelService } from './chat-panel.service';
import { MessagesHubService } from '../../../../core/services/signalR/messages-hub-service';
import { MessageCardComponent } from './components/message-card.component/message-card.component';
import { PositionsEnum } from '../../../../core/enum/positions-enum';
import { MessagesService } from '../../../../core/services/api-services/messages-service';
import { IsVisableDirective } from '../../../../shared/directives/is-visable.directive';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { LoadingComponent } from '../../../../shared/components/loading/loading.component';
import { DrawerComponent } from '../../../../shared/components/drawer/drawer.component';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { HeroIcon } from '../../../../core/constants/hero-icon-d';
@Component({
  selector: 'app-chat-panel',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MessageCardComponent,
    IsVisableDirective,
    LoadingComponent,
    DrawerComponent,
    IconComponent,
  ],
  templateUrl: './chat-panel.component.html',
})
export class ChatPanelComponent implements OnInit, OnDestroy {
  protected readonly HeroIcon = HeroIcon;

  // DI
  protected readonly chatPanelService = inject(ChatPanelService);
  protected readonly messageHubService = inject(MessagesHubService);
  protected readonly messagesService = inject(MessagesService);
  private readonly _destroyRef = inject(DestroyRef);

  // viewChild
  messagesDiv = viewChild.required<ElementRef<HTMLDivElement>>('messagesDiv');

  // signals
  isTyping = signal<boolean>(false);
  readonly isSending = signal(false);

  // protected
  protected messageContent: string = '';

  // constructor
  constructor() {
    this.messagesService.receiveMessages().pipe(takeUntilDestroyed(this._destroyRef)).subscribe();

    toObservable(this.chatPanelService.node)
      .pipe(
        switchMap((node) => {
          this.messageContent = '';
          this.isTyping.set(false);
          if (!node) return EMPTY;
          return this.messagesService.loadMoreMessages(node.userId).pipe(catchError(() => EMPTY));
        }),
        takeUntilDestroyed(this._destroyRef),
      )
      .subscribe();

    // manage page scroll
    let firstCheck = true;
    let secondCheck = true;

    toObservable(this.messagesService.isLoading)
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe({
        next: (isLoading) => {
          if (!isLoading) {
            // if first time finished loading dont do anything (the initial value)
            if (firstCheck) firstCheck = false;
            else {
              // if second time (first time fitching messages) scroll to bottom
              if (secondCheck) {
                this.scrollToBottom();
                secondCheck = false;
              } else {
                // else stay in place
                this.stayInPlace();
              }
            }
          }
        },
      });

    // when receive a message scroll to bottom
    this.messageHubService.onMessageReceived$.pipe(takeUntilDestroyed(this._destroyRef)).subscribe({
      next: () => {
        this.scrollToBottom();
      },
    });

    // clear text input after closing / opening chat panel
    toObservable(this.chatPanelService.isVisable)
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe({
        next: () => {
          this.messageContent = '';
        },
      });
  }

  // methods

  ngOnInit() {
    this.registerHandlers();

    this.scrollToBottom();
  }

  getPositionString(position: PositionsEnum | null) {
    if (position == null) return 'unknown position';
    return PositionsEnum[position] || 'unknown';
  }

  registerHandlers() {
    // on typing
    this.messageHubService.onTyping$.pipe(takeUntilDestroyed(this._destroyRef)).subscribe({
      next: () => {
        this.isTyping.set(true);
      },
    });

    // on stoped typing
    this.messageHubService.onStopTyping$.pipe(takeUntilDestroyed(this._destroyRef)).subscribe({
      next: () => {
        this.isTyping.set(false);
      },
    });
  }

  loadMoreMessages() {
    const userId = this.chatPanelService.node()?.userId;
    if (!userId) return;

    this.messagesService
      .loadMoreMessages(userId)
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe({
        error: () => {},
      });
  }

  handleTyping() {
    if (this.messageContent) this.messageHubService.notifyTyping();
    else this.messageHubService.notifyStoppedTyping();
  }

  handleSendMessage() {
    const content = this.messageContent.trim();
    if (!content || this.isSending()) return;
    this.isSending.set(true);

    const userId = this.chatPanelService.node()?.userId;
    this.messageHubService
      .sendMessage(content)
      .pipe(
        takeUntilDestroyed(this._destroyRef),
        finalize(() => this.isSending.set(false)),
      )
      .subscribe({
        next: () => {
          if (
            this.chatPanelService.node()?.userId === userId &&
            this.messageContent.trim() === content
          ) {
            this.messageContent = '';
          }
          this.messageHubService.notifyStoppedTyping();
          this.scrollToBottom();
        },
        error: () => {},
      });
  }

  scrollToBottom(timeOut: number = 0) {
    setTimeout(() => {
      if (this._destroyRef.destroyed) return;
      const div = this.messagesDiv().nativeElement;
      div.scrollTo({
        top: div.scrollHeight,
        behavior: 'smooth',
      });
    }, timeOut);
  }

  stayInPlace() {
    const div = this.messagesDiv().nativeElement;

    const oldScrollHeight = div.scrollHeight;
    const oldScrollTop = div.scrollTop;

    setTimeout(() => {
      if (this._destroyRef.destroyed) return;
      const newScrollHeight = div.scrollHeight;
      const heightDifference = newScrollHeight - oldScrollHeight;

      div.scrollTop = oldScrollTop + heightDifference;
    }, 0);
  }

  // on destroy
  ngOnDestroy(): void {
    this.messagesService.cancelRequests$.next();
    this.chatPanelService.close();
  }
}
