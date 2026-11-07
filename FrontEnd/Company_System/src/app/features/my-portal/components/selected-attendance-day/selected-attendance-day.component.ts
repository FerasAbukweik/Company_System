import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { AttendanceRecord } from '../../../../core/dto/attendance/attendance-record-dto';
import { toTimeInput, workedHours } from '../../../../core/utils/attendance-time';

@Component({
  selector: 'app-selected-attendance-day',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './selected-attendance-day.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SelectedAttendanceDayComponent {
  readonly date = input.required<string>();
  readonly record = input<AttendanceRecord | null>(null);
  readonly loading = input(false);
  readonly error = input('');
  protected readonly states = ['Worked', 'Paid Leave', 'Holiday', 'Weekend'];
  protected readonly clockIn = computed(
    () => toTimeInput(this.record()?.clockIn ?? null) || 'Not recorded',
  );
  protected readonly clockOut = computed(
    () => toTimeInput(this.record()?.clockOut ?? null) || 'Not recorded',
  );
  protected readonly hours = computed(() =>
    workedHours(this.record()?.clockIn ?? null, this.record()?.clockOut ?? null),
  );
}
