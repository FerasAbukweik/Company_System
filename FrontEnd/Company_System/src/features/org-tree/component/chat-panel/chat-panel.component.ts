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
import { MessagesHubService } from '../../../../core/services/signalR/Messages-hub-service';
import { MessageCardComponent } from './components/message-card.component/message-card.component';
import { PositionsEnum } from '../../../../core/enum/positions-enum';
import { MessagesService } from '../../../../core/services/client/messages-service';
import { IsVisableDirective } from '../../../../shared/directives/is-visable.directive';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { LoadingComponent } from '../../../../shared/components/loading/loading.component';
@Component({
  selector: 'app-chat-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, MessageCardComponent, IsVisableDirective, LoadingComponent],
  templateUrl: './chat-panel.component.html',
})
export class ChatPanelComponent implements OnInit, OnDestroy {
  // DI
  protected readonly chatPanelService = inject(ChatPanelService);
  protected readonly messageHubService = inject(MessagesHubService);
  protected readonly messagesService = inject(MessagesService);
  private readonly _destroyRef = inject(DestroyRef);

  // viewChild
  messagesDiv = viewChild.required<ElementRef<HTMLDivElement>>('messagesDiv');

  // signals
  isTyping = signal<boolean>(false);

  // protected
  protected messageContent: string = '';

  // constructor
  constructor() {
    // manage page scroll
    let firstCheck = true;
    let secondCheck = true;

    const sub = toObservable(this.messagesService.isLoading)
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

  handleTyping() {
    if (this.messageContent) this.messageHubService.notifyTyping();
    else this.messageHubService.notifyStoppedTyping();
  }

  handleSendMessage() {
    this.messageHubService.sendMessage(this.messageContent);
    this.messageContent = '';
    this.messageHubService.notifyStoppedTyping();

    this.scrollToBottom();
  }

  scrollToBottom(timeOut: number = 0) {
    setTimeout(() => {
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
      const newScrollHeight = div.scrollHeight;
      const heightDifference = newScrollHeight - oldScrollHeight;

      div.scrollTop = oldScrollTop + heightDifference;
    }, 0);
  }

  // on destroy
  ngOnDestroy(): void {
    this.messagesService.cancelRequests$.next();
  }
}
