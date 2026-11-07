import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { AuthService } from '../api-services/auth-service';
import { ToastService } from '../api-services/toast-service';
import { SignalRService } from './signalr-service';
import { MessagesHubService } from './messages-hub-service';
import { NotificationHubService } from './notification-hub-service';

const fake = {
  state: 'Disconnected',
  start: vi.fn(),
  stop: vi.fn(),
  invoke: vi.fn(),
  send: vi.fn(),
  handlers: {} as Record<string, (...args: any[]) => void>,
  reconnected: () => {},
  reconnecting: () => {},
  closed: () => {},
};

const connection = {
  get state() {
    return fake.state;
  },
  start: fake.start,
  stop: fake.stop,
  invoke: fake.invoke,
  send: fake.send,
  on: vi.fn((name: string, handler: (...args: any[]) => void) => (fake.handlers[name] = handler)),
  onreconnected: (handler: () => void) => (fake.reconnected = handler),
  onreconnecting: (handler: () => void) => (fake.reconnecting = handler),
  onclose: (handler: () => void) => (fake.closed = handler),
};

describe('One session SignalR connection', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    vi.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(
      connection as unknown as HubConnection,
    );
    fake.state = 'Disconnected';
    fake.handlers = {};
    fake.start.mockImplementation(async () => {
      fake.state = 'Connected';
    });
    fake.stop.mockImplementation(async () => {
      fake.state = 'Disconnected';
    });
    fake.invoke.mockResolvedValue(undefined);
    fake.send.mockResolvedValue(undefined);
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: { userData: signal({ userId: 'me' }), updateTokens: () => of(undefined) },
        },
        { provide: ToastService, useValue: { error: vi.fn() } },
      ],
    });
  });

  afterEach(() => vi.restoreAllMocks());

  it('shares one connection between notifications and chats and does not stop it on chat close', async () => {
    const notifications = TestBed.inject(NotificationHubService);
    const messages = TestBed.inject(MessagesHubService);
    const shared = TestBed.inject(SignalRService);
    await Promise.all([notifications.startConnection(), messages.startConnection('other')]);
    await messages.stopConnection();
    await messages.startConnection('next');
    expect(TestBed.inject(SignalRService).connection).toBe(connection);
    expect(fake.start).toHaveBeenCalledOnce();
    expect(fake.stop).not.toHaveBeenCalled();
    expect(fake.invoke).toHaveBeenCalledWith('RemoveFromGroup', 'other');
    expect(fake.invoke).toHaveBeenCalledWith('AddToGroup', 'next');
    await shared.stop();
    expect(fake.stop).toHaveBeenCalledOnce();
  });

  it('rejoins the active chat before emitting catch-up and keeps notification handlers registered once', async () => {
    const notifications = TestBed.inject(NotificationHubService);
    const messages = TestBed.inject(MessagesHubService);
    const caughtUp = vi.fn();
    const received = vi.fn();
    messages.onReconnected$.subscribe(caughtUp);
    notifications.onNotificationReceived$.subscribe(received);
    await messages.startConnection('other');
    fake.reconnected();
    await vi.waitFor(() => expect(caughtUp).toHaveBeenCalledOnce());
    expect(fake.invoke).toHaveBeenNthCalledWith(2, 'AddToGroup', 'other');
    expect(caughtUp).toHaveBeenCalledOnce();
    fake.handlers['ReceiveNotification']({ id: 'notification' });
    expect(received).toHaveBeenCalledOnce();
    expect(TestBed.inject(SignalRService).connection).toBe(connection);
  });
});
