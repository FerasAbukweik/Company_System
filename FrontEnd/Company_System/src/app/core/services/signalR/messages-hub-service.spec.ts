import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { vi } from 'vitest';
import { MessagesHubService } from './messages-hub-service';
import { ToastService } from '../api-services/toast-service';

describe('Sending chat messages', () => {
  it('reports a disconnected send to the caller instead of silently dropping the message', async () => {
    const toast = { error: vi.fn() };
    TestBed.configureTestingModule({ providers: [{ provide: ToastService, useValue: toast }] });
    const service = TestBed.inject(MessagesHubService);

    await expect(firstValueFrom(service.sendMessage('Please keep this draft'))).rejects.toThrow(
      'Messages hub is not connected',
    );
    expect(toast.error).toHaveBeenCalledOnce();
  });
});
