import { ChangeDetectionStrategy, Component, effect, input, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AttendanceEmployee } from '../../../../../core/dto/attendance/attendance-employee-dto';
import { AttendanceDaySave } from '../../attendance.model';

@Component({
  selector: 'tr[app-attendance-employee-row]',
  imports: [ReactiveFormsModule],
  templateUrl: './attendance-employee-row.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'border-t border-surface-high' },
})
export class AttendanceEmployeeRowComponent {
  readonly employee = input.required<AttendanceEmployee>();
  readonly date = input.required<string>();
  readonly saving = input(false);
  readonly disabled = input(false);
  readonly saved = output<AttendanceDaySave>();

  protected readonly states = ['Worked', 'Paid Leave', 'Holiday', 'Weekend'];
  protected readonly form = new FormGroup({
    hours: new FormControl(8, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0), Validators.max(24)],
    }),
    state: new FormControl(0, { nonNullable: true, validators: [Validators.required] }),
  });

  constructor() {
    effect(() => {
      this.date();
      const record = this.employee().record;
      this.form.reset({
        hours: record ? Number((record.minutesWorked / 60).toFixed(4)) : 8,
        state: record?.state ?? 0,
      });
    });
  }

  protected save() {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.disabled() || this.saving()) return;

    const { hours, state } = this.form.getRawValue();
    this.saved.emit({
      userId: this.employee().userId,
      recordId: this.employee().record?.id,
      minutesWorked: Math.round(hours * 60),
      state,
    });
  }
}
