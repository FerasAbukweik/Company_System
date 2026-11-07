import { Component, DestroyRef, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AttendanceCalendarComponent } from '../../../shared/components/attendance-calendar/attendance-calendar.component';
import { AttendanceDayTableComponent } from './components/attendance-day-table/attendance-day-table.component';
import { AttendanceService } from './attendance.service';
import { AttendanceDaySave } from './attendance.model';

@Component({
  selector: 'app-attendance',
  providers: [AttendanceService],
  imports: [AttendanceCalendarComponent, AttendanceDayTableComponent],
  templateUrl: './attendance.component.html',
  host: { class: 'block p-4 sm:p-6 text-text-primary' },
})
export class AttendanceComponent {
  private readonly service = inject(AttendanceService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly attendancePages = this.service.attendancePages;
  protected readonly totalCount = this.service.totalCount;
  protected readonly loading = this.service.loading;
  protected readonly error = this.service.error;
  protected readonly month = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  protected readonly selectedDate = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(5);
  protected readonly savingUserId = signal<string | null>(null);

  protected selectDate(date: string) {
    if (this.savingUserId()) return;
    this.selectedDate.set(date);
    this.page.set(1);
    this.load();
  }

  protected changeMonth(month: Date) {
    if (this.savingUserId()) return;
    this.month.set(month);
    this.selectedDate.set(null);
    this.page.set(1);
    this.service.clearDay();
  }

  protected changePage(page: number) {
    if (this.savingUserId() || page === this.page()) return;
    this.page.set(page);
    this.load();
  }

  protected changePageSize(pageSize: number) {
    if (this.savingUserId()) return;
    this.pageSize.set(pageSize);
    this.page.set(1);
    this.load();
  }

  protected load() {
    const date = this.selectedDate();
    if (date) this.service.loadDay(date, this.page(), this.pageSize());
  }

  protected save(value: AttendanceDaySave) {
    const date = this.selectedDate();
    if (!date || this.savingUserId()) return;
    this.savingUserId.set(value.userId);

    this.service
      .save(
        {
          date,
          userId: value.userId,
          clockIn: value.clockIn,
          clockOut: value.clockOut,
          state: value.state,
        },
        value.recordId,
      )
      .pipe(
        finalize(() => this.savingUserId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({ error: (error) => this.service.reportError(error) });
  }
}
