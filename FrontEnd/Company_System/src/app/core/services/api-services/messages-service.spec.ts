import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { vi } from 'vitest';
import { MessagesService } from './messages-service';
import { MessagesApiService } from '../api/messages-api-service';
import { MessagesHubService } from '../signalR/messages-hub-service';
import { AuthService } from './auth-service';
import { ChatPanelService } from '../../../features/org-tree/component/chat-panel/chat-panel.service';
import { ToastService } from './toast-service';
import { MessageDTO } from '../../dto/message/message-dto';

describe('Message history', () => {
  const api = { lazyGetMessages: vi.fn() };
  let incoming: Subject<MessageDTO>;
  let service: MessagesService;

  beforeEach(() => {
    api.lazyGetMessages.mockReset();
    incoming = new Subject<MessageDTO>();
    TestBed.configureTestingModule({
      providers: [
        { provide: MessagesApiService, useValue: api },
        { provide: MessagesHubService, useValue: { onMessageReceived$: incoming } },
        { provide: AuthService, useValue: { userData: signal({ userId: 'b' }) } },
        { provide: ChatPanelService, useValue: { node: signal({ userId: 'a' }) } },
        { provide: ToastService, useValue: { error: vi.fn() } },
      ],
    });
    service = TestBed.inject(MessagesService);
  });

  it('returns an empty list before a conversation is loaded', () => {
    expect(service.messages()).toEqual([]);
  });

  it('deduplicates messages received live and in overlapping history pages', () => {
    const message = { id: 'one', groupName: 'b-a', createdAt: new Date() } as MessageDTO;
    const subscription = service.receiveMessages().subscribe();
    incoming.next(message);
    api.lazyGetMessages.mockReturnValue(of([message]));
    service.loadMoreMessages('a').subscribe();
    incoming.next(message);
    expect(service.messages()).toEqual([message]);
    subscription.unsubscribe();
  });

  it('cancels history loads and clears cached conversations on reset', () => {
    const page = new Subject<MessageDTO[]>();
    api.lazyGetMessages.mockReturnValue(page);
    service.loadMoreMessages('a').subscribe();
    service.reset();
    page.next([{ id: 'late', groupName: 'b-a' } as MessageDTO]);
    expect(service.messages()).toEqual([]);
    expect(service.isLoading()).toBe(false);
  });
});
