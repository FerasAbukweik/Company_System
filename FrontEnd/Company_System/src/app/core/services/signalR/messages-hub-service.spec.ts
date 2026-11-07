import { signal } from '@angular/core';
import { SignalRService } from './signalr-service';
import { AuthService } from '../api-services/auth-service';
import { Subject } from 'rxjs';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { vi } from 'vitest';
import { MessagesHubService } from './messages-hub-service';
import { ToastService } from '../api-services/toast-service';

describe('Sending chat messages', () => {
  it('reports a disconnected send to the caller instead of silently dropping the message', async () => {
    const toast = { error: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        { provide: ToastService, useValue: toast },
        { provide: AuthService, useValue: { userData: signal(null) } },
        {
          provide: SignalRService,
          useValue: {
            connection: { on: vi.fn(), state: 'Disconnected' },
            onReconnected$: new Subject(),
          },
        },
      ],
    });
    const service = TestBed.inject(MessagesHubService);

    await expect(firstValueFrom(service.sendMessage('Please keep this draft'))).rejects.toThrow(
      'Messages hub is not connected',
    );
    expect(toast.error).toHaveBeenCalledOnce();
  });
});
