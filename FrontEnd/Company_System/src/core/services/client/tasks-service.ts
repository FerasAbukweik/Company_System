import { inject, Injectable, signal } from '@angular/core';
import { LazyDTO } from '../../dto/lazy-dto';
import { TaskDTO } from '../../dto/task-dto';
import { TasksApiService } from '../api/tasks-api-service';
import { TaskStatusEnum } from '../../enum/task-states-enum';
import { ApprovalService } from './approval-service';
import { ToastService } from './toast-service';

@Injectable({ providedIn: 'root' })
export class TasksService {
  // DI
  private readonly tasksApiService = inject(TasksApiService);
  private readonly approvalService = inject(ApprovalService);
  private readonly toastService = inject(ToastService);

  // private
  private isMoreTasksAvaiable = true;
  private lazyData: LazyDTO = {
    taken: 0,
    sectionSize: 10,
  };

  // signals
  private tasks = signal<TaskDTO[]>([]);
  private isLoading = signal<boolean>(false);

  // getters
  get getTasks() {
    return this.tasks.asReadonly();
  }

  get getIsLoading() {
    return this.isLoading.asReadonly();
  }

  // methods

  reset() {
    this.tasks.set([]);
    this.lazyData.taken = 0;
    this.isLoading.set(false);
    this.isMoreTasksAvaiable = true;
  }

  loadMore() {
    if (this.isLoading() || !this.isMoreTasksAvaiable) return;
    this.isLoading.set(true);

    this.tasksApiService.lazyGetTasks(this.lazyData).subscribe({
      next: (data) => {
        this.tasks.update((curr) => [...curr, ...data]);

        this.lazyData.taken += data.length;
        this.isMoreTasksAvaiable = data.length > 0;
      },
      error: () => {
        this.toastService.error('something went wrong while loading tasks');
      },
      complete: () => {
        this.isLoading.set(false);
      },
    });
  }

  updateTaskStatus(taskId: string, newStatus: TaskStatusEnum) {
    if (this.isLoading()) return;
    this.isLoading.set(true);
    // to restore later in case something went wrong
    const oldTasks = [...this.tasks()];

    // optimistic update
    this.tasks.update((curr) => curr.filter((t) => t.id != taskId));

    this.tasksApiService.updateStatus(taskId, newStatus).subscribe({
      next: () => {
        this.toastService.success('task updated successfully');

        this.approvalService.resetRequested();
      },
      error: () => {
        this.tasks.set(oldTasks);

        this.toastService.error('something went wrong while updating task');
      },
      complete: () => {
        this.isLoading.set(false);
      },
    });
  }
}
