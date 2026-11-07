import { ChangeDetectionStrategy, Component, effect, input, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { currentTime, toTimeInput, toTimeValue } from '../../../../../core/utils/attendance-time';
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
    clockIn: new FormControl('', { nonNullable: true }),
    clockOut: new FormControl({ value: '', disabled: true }, { nonNullable: true }),
    state: new FormControl(0, { nonNullable: true, validators: [Validators.required] }),
  });

  constructor() {
    this.form.controls.clockIn.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.updateClockOut());

    effect(() => {
      this.date();
      const record = this.employee().record;
      this.form.reset({
        clockIn: toTimeInput(record?.clockIn ?? null),
        clockOut: toTimeInput(record?.clockOut ?? null),
        state: record?.state ?? 0,
      });
      this.updateClockOut();
    });
  }

  protected defaultTime(field: 'clockIn' | 'clockOut') {
    const control = this.form.controls[field];
    if (!control.disabled && !control.value) control.setValue(currentTime());
  }

  private updateClockOut() {
    const clockOut = this.form.controls.clockOut;
    if (this.form.controls.clockIn.value) clockOut.enable({ emitEvent: false });
    else {
      clockOut.reset('', { emitEvent: false });
      clockOut.disable({ emitEvent: false });
    }
  }

  protected save() {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.disabled() || this.saving()) return;

    const { clockIn, clockOut, state } = this.form.getRawValue();
    this.saved.emit({
      userId: this.employee().userId,
      recordId: this.employee().record?.id,
      clockIn: toTimeValue(clockIn),
      clockOut: toTimeValue(clockOut),
      state,
    });
  }
}
