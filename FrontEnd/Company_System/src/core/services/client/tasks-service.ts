import { inject, Injectable, signal } from '@angular/core';
import { LazyDTO } from '../../dto/lazy-dto';
import { TaskDTO } from '../../dto/task-dto';
import { TasksApiService } from '../api/tasks-api-service';
import { TaskStatusEnum } from '../../enum/task-states-enum';

@Injectable({ providedIn: 'root' })
export class TasksService {
  // DI
  private readonly tasksApiService = inject(TasksApiService);

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
        this.isLoading.set(false);
        this.isMoreTasksAvaiable = data.length > 0;
      },
      error: (err) => {
        // TODO show error toast

        this.isLoading.set(false);
      },
    });
  }

  updateTaskStatus(taskId: string, newStatus: TaskStatusEnum) {
    // to restore later in case something went wrong
    const oldTasks = [...this.tasks()];

    // optimistic update
    this.tasks.update((curr) => curr.filter(t => t.id != taskId));

    this.tasksApiService.updateStatus(taskId, newStatus).subscribe({
      next: () => {
        // TODO show toast
      },
      error: () => {
        this.tasks.set(oldTasks);

        // TODO show error
      },
    });
  }
}
