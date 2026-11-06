import { apiError } from '../../../core/utils/api-error';
import { AttendanceRecord } from '../../../core/dto/attendance/attendance-record-dto';
import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { finalize, Subscription, tap } from 'rxjs';
import { DailyWorkRecordsService } from '../../../core/services/api-services/daily-work-records-service';
import { AttendanceEmployee } from '../../../core/dto/attendance/attendance-employee-dto';

@Injectable()
export class AttendanceService {
  private readonly workRecordsService = inject(DailyWorkRecordsService);
  private readonly destroyRef = inject(DestroyRef);
  private pageRequest?: Subscription;
  private currentDate = '';
  private currentPageSize = 5;

  readonly attendancePages = signal<Record<number, AttendanceEmployee[]>>({});
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal('');

  constructor() {
    this.destroyRef.onDestroy(() => this.pageRequest?.unsubscribe());
  }

  loadDay(date: string, page: number, pageSize: number) {
    this.pageRequest?.unsubscribe();
    if (date !== this.currentDate || pageSize !== this.currentPageSize) {
      this.attendancePages.set({});
      this.totalCount.set(0);
    }
    this.currentDate = date;
    this.currentPageSize = pageSize;
    this.error.set('');
    if (this.attendancePages()[page]?.length) return;
    this.loading.set(true);

    this.pageRequest = this.workRecordsService
      .attendanceDayRecords(date, { taken: (page - 1) * pageSize, sectionSize: pageSize })
      .pipe(
        tap((result) => {
          this.attendancePages.update((pages) => ({ ...pages, [page]: result.items }));
          this.totalCount.set(result.totalCount);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({ error: (error) => this.reportError(error) });
  }

  clearDay() {
    this.pageRequest?.unsubscribe();
    this.currentDate = '';
    this.attendancePages.set({});
    this.totalCount.set(0);
    this.loading.set(false);
    this.error.set('');
  }

  save(data: Omit<AttendanceRecord, 'id'>, id?: string) {
    this.error.set('');
    return this.workRecordsService.saveAttendance(data, id).pipe(
      tap((record) => {
        if (this.currentDate === record.date) {
          this.attendancePages.update((pages) =>
            Object.fromEntries(
              Object.entries(pages).map(([page, result]) => [
                page,
                result.map((employee) =>
                  employee.userId === record.userId ? { ...employee, record } : employee,
                ),
              ]),
            ),
          );
        }
      }),
    );
  }

  reportError(error: unknown) {
    this.error.set(apiError(error));
  }
}
