import { ToastService } from './toast-service';
import { toastError } from '../../utils/toast-error';
import { inject, Injectable, signal } from '@angular/core';
import { TaskDTO } from '../../dto/task/task-dto';
import { TasksApiService } from '../api/tasks-api-service';
import { TaskStatusEnum } from '../../enum/task-states-enum';
import { ApprovalService } from './approval-service';
import { EMPTY, Subject, defer, finalize, takeUntil, tap } from 'rxjs';
import { TaskAddDTO } from '../../dto/task/task-add-dto';

@Injectable({ providedIn: 'root' })
export class TasksService {
  private readonly _toastService = inject(ToastService);

  // DI
  private readonly _tasksApiService = inject(TasksApiService);
  private readonly _approvalService = inject(ApprovalService);

  // private
  readonly sectionSize = 10;
  private _isMoreTasksAvaiable = true;

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
    this._isLoading.set(false);
    this._isMoreTasksAvaiable = true;
  }

  loadMore() {
    return defer(() => {
      if (this._isLoading() || !this._isMoreTasksAvaiable) return EMPTY;
      this._isLoading.set(true);

      return this._tasksApiService
        .lazyGetTasks({ taken: this._tasks().length, sectionSize: this.sectionSize })
        .pipe(toastError(this._toastService, 'تعذر تحميل المهام'), takeUntil(this.cancelRequest$))
        .pipe(
          tap({
            next: (data) => {
              this._tasks.update((curr) => [...curr, ...data]);

              this._isMoreTasksAvaiable = data.length > 0;
            },
          }),
          finalize(() => {
            this._isLoading.set(false);
          }),
        );
    });
  }

  updateTaskStatus(taskId: string, newStatus: TaskStatusEnum) {
    return defer(() => {
      if (this._isLoading()) return EMPTY;
      this._isLoading.set(true);
      // to restore later in case something went wrong
      const oldTasks = [...this._tasks()];

      // optimistic update
      this._tasks.update((curr) => curr.filter((t) => t.id != taskId));

      return this._tasksApiService.updateStatus(taskId, newStatus).pipe(
        toastError(this._toastService, 'تعذر تحديث المهمة'),
        takeUntil(this.cancelRequest$),
        tap({
          next: () => {
            this._approvalService.resetRequested();
          },
          error: () => {
            this._tasks.set(oldTasks);
          },
        }),
        finalize(() => {
          this._isLoading.set(false);
        }),
      );
    });
  }

  addTask(taskData: TaskAddDTO) {
    return this._tasksApiService
      .addTask(taskData)
      .pipe(toastError(this._toastService, 'تعذر إضافة المهمة'));
  }
}
