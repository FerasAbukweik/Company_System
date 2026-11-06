import { TestBed } from '@angular/core/testing';
import { Subject, of } from 'rxjs';
import { TasksService } from './tasks-service';
import { ToastService } from './toast-service';
import { TasksApiService } from '../api/tasks-api-service';
import { ApprovalService } from './approval-service';
import { TaskDTO } from '../../dto/task/task-dto';
import { TaskStatusEnum } from '../../enum/task-states-enum';
import { vi } from 'vitest';

describe('TasksService observable contract', () => {
  let service: TasksService;
  let api: { lazyGetTasks: ReturnType<typeof vi.fn>; updateStatus: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = { lazyGetTasks: vi.fn(), updateStatus: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        TasksService,
        { provide: ToastService, useValue: { error: vi.fn() } },
        { provide: TasksApiService, useValue: api },
        { provide: ApprovalService, useValue: { resetRequested: vi.fn() } },
      ],
    });
    service = TestBed.inject(TasksService);
  });

  it('waits for subscription and derives the next offset from stored tasks', () => {
    api.lazyGetTasks.mockReturnValue(of([{ id: 'one' }, { id: 'two' }] as TaskDTO[]));
    const request = service.loadMore();
    expect(api.lazyGetTasks).not.toHaveBeenCalled();
    expect(service.isLoading()).toBe(false);
    request.subscribe();
    api.lazyGetTasks.mockReturnValue(of([]));
    service.loadMore().subscribe();
    expect(api.lazyGetTasks).toHaveBeenLastCalledWith({
      taken: 2,
      sectionSize: service.sectionSize,
    });
    expect(service.tasks()).toHaveLength(2);
    expect(service.isLoading()).toBe(false);
  });

  it('propagates errors to callers and allows retry after failure', () => {
    const response = new Subject<TaskDTO[]>();
    api.lazyGetTasks.mockReturnValue(response);
    const onError = vi.fn();
    service.loadMore().subscribe({ error: onError });
    service.loadMore().subscribe();
    expect(api.lazyGetTasks).toHaveBeenCalledTimes(1);
    const failure = new Error('request failed');
    response.error(failure);
    expect(onError).toHaveBeenCalledWith(failure);
    expect(service.isLoading()).toBe(false);
    api.lazyGetTasks.mockReturnValue(of([]));
    service.loadMore().subscribe();
    expect(api.lazyGetTasks).toHaveBeenCalledTimes(2);
  });

  it('clears loading when a caller unsubscribes or a request is cancelled', () => {
    api.lazyGetTasks.mockReturnValue(new Subject<TaskDTO[]>());
    const subscription = service.loadMore().subscribe();
    expect(service.isLoading()).toBe(true);
    subscription.unsubscribe();
    expect(service.isLoading()).toBe(false);
    service.loadMore().subscribe();
    service.cancelRequest$.next();
    expect(service.isLoading()).toBe(false);
  });

  it('restores optimistic changes and exposes mutation errors to the caller', () => {
    const tasks = [{ id: 'one' }, { id: 'two' }] as TaskDTO[];
    api.lazyGetTasks.mockReturnValue(of(tasks));
    service.loadMore().subscribe();
    const response = new Subject<void>();
    api.updateStatus.mockReturnValue(response);
    const onError = vi.fn();
    service.updateTaskStatus('one', TaskStatusEnum.Completed).subscribe({ error: onError });
    expect(service.tasks().map((task) => task.id)).toEqual(['two']);
    const failure = new Error('update failed');
    response.error(failure);
    expect(service.tasks()).toEqual(tasks);
    expect(onError).toHaveBeenCalledWith(failure);
    expect(service.isLoading()).toBe(false);
  });
  it('does not restore an old session after a pending status change is cancelled', () => {
    api.lazyGetTasks.mockReturnValue(of([{ id: 'one' }] as TaskDTO[]));
    service.loadMore().subscribe();
    const response = new Subject<void>();
    api.updateStatus.mockReturnValue(response);
    service.updateTaskStatus('one', TaskStatusEnum.Completed).subscribe();
    service.reset();
    response.error(new Error('Late response from previous session'));
    expect(service.tasks()).toEqual([]);
    expect(service.isLoading()).toBe(false);
  });
});
