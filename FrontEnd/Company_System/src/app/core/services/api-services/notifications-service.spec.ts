import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { Subject } from 'rxjs';
import { NotificationDTO } from '../../dto/notification/notification-dto';
import { NotificationHubService } from '../signalR/notification-hub-service';
import { SignalRService } from '../signalR/signalr-service';
import { NotificationsService } from './notifications-service';

const item: NotificationDTO = {
  id: 'one',
  userId: 'user',
  title: 'Task',
  content: 'Assigned',
  targetPath: '/dashboard',
  createdAt: '2026-11-07T08:00:00Z',
  isRead: false,
  type: 0,
};

const batch = Array.from({ length: 10 }, (_, index) => ({ ...item, id: `notification-${index}` }));

describe('Notification list', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: NotificationHubService,
          useValue: {
            onNotificationReceived$: new Subject(),
            onReconnected$: new Subject(),
            startConnection: async () => {},
          },
        },
        { provide: SignalRService, useValue: { state: signal('connected') } },
      ],
    }),
  );
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('appends batches using the actual list length and avoids duplicate requests', () => {
    const service = TestBed.inject(NotificationsService);
    const http = TestBed.inject(HttpTestingController);
    service.loadMore();
    service.loadMore();
    expect(service.notifications()).toEqual([]);
    const first = http.expectOne((request) => request.method === 'GET');
    expect(first.request.params.get('taken')).toBe('0');
    expect(first.request.params.get('sectionSize')).toBe('10');
    first.flush({ items: batch, totalCount: 2, unreadCount: 1 });
    expect(service.isMoreAvaiable()).toBe(true);

    service.loadMore();
    const second = http.expectOne((request) => request.method === 'GET');
    expect(second.request.params.get('taken')).toBe('10');
    expect(service.notifications()).toEqual(batch);
    second.flush({ items: [{ ...item, id: 'two' }], totalCount: 2, unreadCount: 1 });
    service.loadMore();
    http.expectNone(() => true);
    expect(service.notifications().length).toBe(11);
    expect(service.isMoreAvaiable()).toBe(false);
    service.reset();
    expect(service.notifications()).toEqual([]);
  });

  it('prepends live notifications and increments the offset and unread count without refetching', () => {
    const service = TestBed.inject(NotificationsService);
    const http = TestBed.inject(HttpTestingController);
    const hub = TestBed.inject(NotificationHubService);
    service.start();
    http.expectOne((request) => request.url.endsWith('/GetUnreadCount')).flush(1);
    service.loadMore();
    http
      .expectOne((request) => request.method === 'GET')
      .flush({ items: batch, totalCount: 3, unreadCount: 1 });

    const incoming = { ...item, id: 'incoming' };
    hub.onNotificationReceived$.next(incoming);
    expect(service.notifications()).toEqual([incoming, ...batch]);
    expect(service.unreadCount()).toBe(2);
    http.expectNone(() => true);
    hub.onNotificationReceived$.next(incoming);
    expect(service.unreadCount()).toBe(2);

    service.loadMore();
    const next = http.expectOne((request) => request.method === 'GET');
    expect(next.request.params.get('taken')).toBe('11');
    next.flush({ items: [{ ...item, id: 'older' }], totalCount: 4, unreadCount: 2 });
    expect(service.notifications().map((item) => item.id)).toEqual([
      'incoming',
      ...batch.map((item) => item.id),
      'older',
    ]);
  });

  it('marks all read only when unread count is positive and shares pending requests', () => {
    const service = TestBed.inject(NotificationsService);
    const http = TestBed.inject(HttpTestingController);
    service.markAllRead().subscribe();
    http.expectNone(() => true);
    service.unreadCount.set(1);
    service.loadMore();
    http
      .expectOne((request) => request.method === 'GET')
      .flush({ items: [item], totalCount: 1, unreadCount: 1 });

    service.markAllRead().subscribe();
    service.markAllRead().subscribe();
    http.expectOne((request) => request.url.endsWith('/MarkAllRead')).flush(true);
    expect(service.unreadCount()).toBe(0);
    expect(service.notifications()[0].isRead).toBe(true);
    service.markAllRead().subscribe();
    http.expectNone(() => true);
  });

  it('preserves notifications received while marking the previous notifications read', () => {
    const service = TestBed.inject(NotificationsService);
    const http = TestBed.inject(HttpTestingController);
    const hub = TestBed.inject(NotificationHubService);
    service.start();
    http.expectOne((request) => request.url.endsWith('/GetUnreadCount')).flush(1);
    service.loadMore();
    http
      .expectOne((request) => request.method === 'GET')
      .flush({ items: [item], totalCount: 1, unreadCount: 1 });
    service.markAllRead().subscribe();
    const mark = http.expectOne((request) => request.method === 'PUT');
    hub.onNotificationReceived$.next({ ...item, id: 'new' });
    mark.flush(true);
    expect(service.unreadCount()).toBe(1);
    expect(service.notifications()[0].isRead).toBe(false);
    expect(service.notifications()[1].isRead).toBe(true);
    service.markAllRead().subscribe();
    http.expectOne((request) => request.method === 'PUT').flush(true);
    expect(service.unreadCount()).toBe(0);
  });

  it('keeps the list on failure and retries from its length', () => {
    const service = TestBed.inject(NotificationsService);
    const http = TestBed.inject(HttpTestingController);
    service.loadMore();
    http
      .expectOne((request) => request.method === 'GET')
      .flush({ items: batch, totalCount: 2, unreadCount: 1 });
    service.loadMore();
    http
      .expectOne((request) => request.method === 'GET')
      .flush({}, { status: 500, statusText: 'Failed' });
    expect(service.notifications()).toEqual(batch);
    expect(service.loadFailed()).toBe(true);
    service.loadMore();
    const retry = http.expectOne((request) => request.method === 'GET');
    expect(retry.request.params.get('taken')).toBe('10');
    retry.flush({ items: [{ ...item, id: 'two' }], totalCount: 2, unreadCount: 1 });
    expect(service.loadFailed()).toBe(false);
  });
  it('does not let a historical batch restore an unread count after marking all read', () => {
    const service = TestBed.inject(NotificationsService);
    const http = TestBed.inject(HttpTestingController);
    service.start();
    http.expectOne((request) => request.url.endsWith('/GetUnreadCount')).flush(1);
    service.loadMore();
    http.expectOne((request) => request.method === 'GET').flush({ items: batch, unreadCount: 1 });
    service.loadMore();
    const history = http.expectOne((request) => request.method === 'GET');
    service.markAllRead().subscribe();
    http.expectOne((request) => request.method === 'PUT').flush(true);
    history.flush({ items: [item], unreadCount: 1 });
    expect(service.unreadCount()).toBe(0);
  });

  it('reconciles the initial count if a live notification arrives while it is loading', () => {
    const service = TestBed.inject(NotificationsService);
    const http = TestBed.inject(HttpTestingController);
    service.start();
    const initialCount = http.expectOne((request) => request.url.endsWith('/GetUnreadCount'));
    TestBed.inject(NotificationHubService).onNotificationReceived$.next(item);
    initialCount.flush(0);
    http.expectOne((request) => request.url.endsWith('/GetUnreadCount')).flush(1);
    expect(service.unreadCount()).toBe(1);
  });
});
