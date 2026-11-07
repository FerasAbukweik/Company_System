import { workedHours } from '../../../core/utils/attendance-time';
import { AttendanceRecord } from '../../../core/dto/attendance/attendance-record-dto';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  model,
  OnInit,
  output,
} from '@angular/core';

import { HeroIcon } from '../../../core/constants/hero-icon-d';
import { IconComponent } from '../icon/icon.component';

@Component({
  selector: 'app-attendance-calendar',
  imports: [IconComponent],
  templateUrl: './attendance-calendar.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendanceCalendarComponent implements OnInit {
  protected readonly HeroIcon = HeroIcon;
  readonly records = input<readonly AttendanceRecord[]>([]);
  readonly month = input(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  readonly monthChanged = output<Date>();
  readonly dateSelected = output<string>();
  readonly selectedDate = model<string | null>(null);
  readonly disabled = input(false);
  readonly showAttendance = input(true);
  protected readonly title = computed(() =>
    this.month().toLocaleDateString('en-US', { month: 'long', year: 'numeric' }),
  );
  protected readonly legend = [
    { label: 'Worked', color: 'bg-vibrant-turquoise' },
    { label: 'Paid Leave', color: 'bg-secondary-blue' },
    { label: 'Holiday', color: 'bg-surface-dim' },
    { label: 'Weekend', color: 'bg-outline-strong' },
  ];
  protected readonly days = computed(() => {
    const month = this.month();
    const offset = month.getDay();
    const count = new Date(month.getFullYear(), month.getMonth() + 1, 0).getDate();
    return Array.from({ length: Math.ceil((offset + count) / 7) * 7 }, (_, index) => {
      const date = new Date(month.getFullYear(), month.getMonth(), index - offset + 1);
      const number = date.getDate();
      const current = date.getMonth() === month.getMonth();
      const dateKey = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(number).padStart(2, '0')}`;
      const record = current ? this.records().find((record) => record.date === dateKey) : undefined;
      const hours = record ? workedHours(record.clockIn, record.clockOut) : null;
      const labels = ['Worked', 'Paid Leave', 'Holiday', 'Weekend'];
      const colors = [
        'bg-turquoise-tint text-turquoise-text',
        'bg-blue-tint text-secondary-blue',
        'bg-surface-dim text-corporate-blue',
        'bg-surface-low text-outline-strong',
      ];
      return {
        key: date.getTime(),
        date: dateKey,
        current,
        number,
        overtime: false,
        description: !current
          ? 'Outside current month'
          : !this.showAttendance()
            ? 'Select attendance date'
            : record
              ? `${labels[record.state]} · ${hours === null ? 'Clock times incomplete' : `${hours.toLocaleString('en-US', { maximumFractionDigits: 2 })} hours`}`
              : 'No attendance data',
        classes: !current
          ? 'text-outline-strong/40'
          : record
            ? colors[record.state]
            : 'text-text-secondary',
      };
    });
  });
  ngOnInit() {
    if (this.selectedDate() !== null) return;

    const today = new Date();
    const date = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;
    this.selectedDate.set(date);
    this.dateSelected.emit(date);
  }

  protected selectDate(date: string) {
    this.selectedDate.set(date);
    this.dateSelected.emit(date);
  }

  protected move(offset: number) {
    const date = this.month();
    this.monthChanged.emit(new Date(date.getFullYear(), date.getMonth() + offset, 1));
  }
}
