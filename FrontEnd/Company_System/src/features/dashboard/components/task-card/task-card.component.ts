import { Component, inject, input, output, signal } from '@angular/core';
import { TaskDTO } from '../../../../core/dto/task-dto';
import { TaskStatusEnum } from '../../../../core/enum/task-states-enum';
import { TaskPrioritiesEnum } from '../../../../core/enum/priorities-enum';
import { DatePipe } from '@angular/common';
import { TaskCardService } from './task-card.service';
import { DropDownMenuComponent } from '../../../../shared/components/drop-down-menu/drop-down-menu.component';
import { TasksService } from '../../../../core/services/client/tasks-service';
import { ToastService } from '../../../../core/services/client/toast-service';

@Component({
  selector: 'app-task-card',
  imports: [DatePipe, DropDownMenuComponent],
  templateUrl: './task-card.component.html',
  host: {
    class:
      'group relative pr-18 flex gap-y-2 flex-wrap items-center justify-between p-4 bg-surface-base border border-outline-light rounded-lg hover:border-vibrant-turquoise transition-colors border-l-4',
  },
})
export class TaskCardComponent {
  // DI
  protected readonly taskCardService = inject(TaskCardService);
  private readonly tasksService = inject(TasksService);
  private readonly toastService = inject(ToastService);

  // input
  task = input.required<TaskDTO>();

  // signals
  showTaskDesctiption = output<TaskDTO>();

  // methods

  // convert TaskStatusEnum to string
  getStatusString(val: TaskStatusEnum) {
    return TaskStatusEnum[val];
  }

  // get priority name
  getPriorityName(priority: TaskPrioritiesEnum) {
    return TaskPrioritiesEnum[priority];
  }

  // on select new task status
  handleChooseMenuOption(taskId: string, optionIdx: number) {
    this.taskCardService.closeStatusMenu();

    switch (optionIdx) {
      case 0:
        this.tasksService.updateTaskStatus(taskId, TaskStatusEnum.Completed);
        break;
      case 1:
        this.showTaskDesctiption.emit(this.task());
        break;
      default:
        this.toastService.error('unhandled menu options');
    }
  }
}
