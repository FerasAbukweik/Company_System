import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { vi } from 'vitest';
import { ChatPanelComponent } from './chat-panel.component';
import { ChatPanelService } from './chat-panel.service';
import { MessagesService } from '../../../../core/services/api-services/messages-service';
import { MessagesHubService } from '../../../../core/services/signalR/messages-hub-service';

describe('Chat message submission', () => {
  let component: ChatPanelComponent;
  let response: Subject<void>;
  const hub = {
    sendMessage: vi.fn(),
    notifyStoppedTyping: vi.fn(),
    onMessageReceived$: new Subject(),
  };

  beforeEach(() => {
    response = new Subject<void>();
    hub.sendMessage.mockReset().mockReturnValue(response);
    hub.notifyStoppedTyping.mockReset();
    TestBed.configureTestingModule({
      imports: [ChatPanelComponent],
      providers: [
        {
          provide: ChatPanelService,
          useValue: {
            node: signal({ userId: 'recipient' }),
            isVisable: signal(true),
            close: vi.fn(),
          },
        },
        {
          provide: MessagesService,
          useValue: {
            resetConversation: vi.fn(),
            receiveMessages: () => new Subject(),
            loadMoreMessages: () => of([]),
            isLoading: signal(false),
            cancelRequests$: new Subject(),
          },
        },
        { provide: MessagesHubService, useValue: hub },
      ],
    });
    component = TestBed.createComponent(ChatPanelComponent).componentInstance;
    vi.spyOn(component, 'scrollToBottom').mockImplementation(() => {});
  });

  it('retains a draft when sending fails', () => {
    component['messageContent'] = 'Keep this draft';
    component.handleSendMessage();
    response.error(new Error('Disconnected'));
    expect(component['messageContent']).toBe('Keep this draft');
    expect(component.isSending()).toBe(false);
  });

  it('blocks duplicate submissions and clears the draft only after successful delivery', () => {
    component['messageContent'] = 'Send once';
    component.handleSendMessage();
    component.handleSendMessage();
    expect(hub.sendMessage).toHaveBeenCalledTimes(1);
    expect(component['messageContent']).toBe('Send once');
    response.next();
    response.complete();
    expect(component['messageContent']).toBe('');
    expect(component.isSending()).toBe(false);
  });
});
