import { Component, inject, input, output } from '@angular/core';
import { TaskDTO } from '../../../../core/dto/task/task-dto';
import { TaskStatusEnum } from '../../../../core/enum/task-states-enum';
import { TaskPrioritiesEnum } from '../../../../core/enum/priorities-enum';
import { DatePipe } from '@angular/common';
import { TaskCardService } from './task-card.service';
import { DropDownMenuComponent } from '../../../../shared/components/drop-down-menu/drop-down-menu.component';
import { TasksService } from '../../../../core/services/api-services/tasks-service';
import { ToastService } from '../../../../core/services/api-services/toast-service';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { HeroIcon } from '../../../../core/constants/hero-icon-d';

@Component({
  selector: 'app-task-card',
  imports: [DatePipe, DropDownMenuComponent, IconComponent],
  templateUrl: './task-card.component.html',
  host: {
    class:
      'group relative pr-18 flex gap-y-2 flex-wrap items-center justify-between p-4 bg-surface-base border border-outline-light rounded-lg hover:border-vibrant-turquoise transition-colors border-l-4',
  },
})
export class TaskCardComponent {
  protected readonly HeroIcon = HeroIcon;

  // DI
  protected readonly taskCardService = inject(TaskCardService);
  private readonly _tasksService = inject(TasksService);
  private readonly _toastService = inject(ToastService);

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
        this._tasksService.updateTaskStatus(taskId, TaskStatusEnum.Completed).subscribe({
          next: () => this._toastService.success('task updated successfully'),
          error: () => {},
        });
        break;
      case 1:
        this.showTaskDesctiption.emit(this.task());
        break;
      default:
        return;
    }
  }
}
