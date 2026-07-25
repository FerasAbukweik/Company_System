import { inject, Injectable, signal } from '@angular/core';
import { LazyDTO } from '../../dto/lazy-dto';
import { TaskDTO } from '../../dto/task-dto';
import { TasksApiService } from '../api/tasks-api-service';
import { TaskStatusEnum } from '../../enum/task-states-enum';
import { ApprovalService } from './approval-service';
import { ToastService } from './toast-service';
import { Subject, takeUntil } from 'rxjs';
import { TaskAddDTO } from '../../dto/task-add-dto';

@Injectable({ providedIn: 'root' })
export class TasksService {
  // DI
  private readonly _tasksApiService = inject(TasksApiService);
  private readonly _approvalService = inject(ApprovalService);
  private readonly _toastService = inject(ToastService);

  // private
  private _isMoreTasksAvaiable = true;
  private _lazyData: LazyDTO = {
    taken: 0,
    sectionSize: 10,
  };

  // objects
  readonly cancelRequest$ = new Subject<void>();

  // signals
  private _tasks = signal<TaskDTO[]>([]);
  private _isLoading = signal<boolean>(false);

  // getters
  get tasks() {
    return this._tasks.asReadonly();
  }

  get isLoading() {
    return this._isLoading.asReadonly();
  }

  // methods

  reset() {
    this.cancelRequest$.next();

    this._tasks.set([]);
    this._lazyData.taken = 0;
    this._isLoading.set(false);
    this._isMoreTasksAvaiable = true;
  }

  loadMore() {
    if (this._isLoading() || !this._isMoreTasksAvaiable) return;
    this._isLoading.set(true);

    this._tasksApiService
      .lazyGetTasks(this._lazyData)
      .pipe(takeUntil(this.cancelRequest$))
      .subscribe({
        next: (data) => {
          this._tasks.update((curr) => [...curr, ...data]);

          this._lazyData.taken += data.length;
          this._isMoreTasksAvaiable = data.length > 0;
          this._isLoading.set(false);
        },
        error: () => {
          this._toastService.error('something went wrong while loading tasks');
          this._isLoading.set(false);
        },
      });
  }

  updateTaskStatus(taskId: string, newStatus: TaskStatusEnum) {
    if (this._isLoading()) return;
    this._isLoading.set(true);
    // to restore later in case something went wrong
    const oldTasks = [...this._tasks()];

    // optimistic update
    this._tasks.update((curr) => curr.filter((t) => t.id != taskId));

    this._tasksApiService.updateStatus(taskId, newStatus).subscribe({
      next: () => {
        this._toastService.success('task updated successfully');

        this._approvalService.resetRequested();
        this._isLoading.set(false);
      },
      error: () => {
        this._tasks.set(oldTasks);

        this._isLoading.set(false);
        this._toastService.error('something went wrong while updating task');
      },
    });
  }

  addTask(taskData: TaskAddDTO) {
    this._tasksApiService.addTask(taskData).subscribe({
      next: () => {
        this._toastService.success('Task Addedd Successfully');
      },
      error: () => {
        this._toastService.error('failed to add task');
      },
    });
  }
}
