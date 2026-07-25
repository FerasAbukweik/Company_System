import { Component, effect, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TaskPrioritiesEnum } from '../../../../core/enum/priorities-enum';
import { DelegateTaskService } from './delegate-task.service';
import { TasksService } from '../../../../core/services/client/tasks-service';
import { TaskAddDTO } from '../../../../core/dto/task-add-dto';

@Component({
  selector: 'app-delegate-task',
  imports: [ReactiveFormsModule],
  templateUrl: './delegate-task.component.html',
})
export class DelegateTaskComponent {
  // DI
  protected readonly delegateTaskService = inject(DelegateTaskService);
  protected readonly tasksService = inject(TasksService);

  // fields
  public readonly todayStr = new Date().toISOString().substring(0, 10);

  // Form
  public readonly form = new FormGroup({
    title: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    deadline: new FormControl(new Date().toISOString().substring(0, 10), {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  // Signals
  public readonly selectedPriority = signal<TaskPrioritiesEnum>(TaskPrioritiesEnum.Low);

  public readonly prioritiesList = Object.values(TaskPrioritiesEnum).filter(
    (p) => typeof p !== 'number',
  );

  constructor() {
    effect(() => {
      if (!this.delegateTaskService.isShowDelegateTask()) {
        this.resetComponentState();
      }
    });
  }

  isError(control: FormControl) {
    return control.invalid && control.touched && control.dirty;
  }

  private resetComponentState(): void {
    this.form.reset({
      title: '',
      description: '',
      deadline: new Date().toISOString().substring(0, 10),
    });
    this.selectedPriority.set(TaskPrioritiesEnum.Low);
  }

  public onSubmit(): void {
    this.form.markAllAsTouched();
    this.form.markAllAsDirty();
    const currentNode = this.delegateTaskService.node();

    if (this.form.invalid || !currentNode || !currentNode.userId) {
      return;
    }

    const formValues = this.form.getRawValue();

    const taskData: TaskAddDTO = {
      userId: currentNode.userId,
      deadline: new Date(formValues.deadline),
      description: formValues.description,
      priority: this.selectedPriority(),
      title: formValues.title,
    };

    this.tasksService.addTask(taskData);

    this.delegateTaskService.close();
  }
}
