import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TaskPrioritiesEnum } from '../../../../core/enum/priorities-enum';
import { DelegateTaskService } from './delegate-task.service';
import { TasksService } from '../../../../core/services/api-services/tasks-service';
import { TaskAddDTO } from '../../../../core/dto/task/task-add-dto';
import { HeroIcon } from '../../../../core/constants/hero-icon-d';
import { DrawerComponent } from '../../../../shared/components/drawer/drawer.component';
import { IconComponent } from '../../../../shared/components/icon/icon.component';

@Component({
  selector: 'app-delegate-task',
  imports: [ReactiveFormsModule, DrawerComponent, IconComponent],
  templateUrl: './delegate-task.component.html',
})
export class DelegateTaskComponent {
  protected readonly HeroIcon = HeroIcon;

  // DI
  protected readonly delegateTaskService = inject(DelegateTaskService);
  protected readonly tasksService = inject(TasksService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly saving = signal(false);

  // fields
  public readonly todayStr = this.today();

  // Form
  public readonly form = new FormGroup({
    title: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    deadline: new FormControl(this.today(), {
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

  private today(): string {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
  }

  private resetComponentState(): void {
    this.form.reset({
      title: '',
      description: '',
      deadline: this.today(),
    });
    this.selectedPriority.set(TaskPrioritiesEnum.Low);
  }

  public onSubmit(): void {
    if (this.saving()) return;

    this.form.markAllAsTouched();
    this.form.markAllAsDirty();
    const currentNode = this.delegateTaskService.node();

    if (this.form.invalid || !currentNode || !currentNode.userId) {
      return;
    }

    const formValues = this.form.getRawValue();

    const taskData: TaskAddDTO = {
      userId: currentNode.userId,
      deadline: new Date(`${formValues.deadline}T23:59:59.999`),
      description: formValues.description,
      priority: this.selectedPriority(),
      title: formValues.title,
    };

    this.saving.set(true);
    this.tasksService
      .addTask(taskData)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: () => this.delegateTaskService.close(),
        // TasksService displays the error; leave the form open so the user can retry.
        error: () => {},
      });
  }
}
