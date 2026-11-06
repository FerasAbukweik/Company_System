import { LazyDTO } from '../../dto/lazy-loading/lazy-dto';
import { AttendanceDayPage } from '../../dto/attendance/attendance-day-page-dto';
import { inject, Injectable } from '@angular/core';
import { defer, of, tap } from 'rxjs';
import { DailyWorkRecordsApiService } from '../api/daily-work-records-api-service';
import { AttendanceRecord } from '../../dto/attendance/attendance-record-dto';

@Injectable({ providedIn: 'root' })
export class DailyWorkRecordsService {
  private readonly api = inject(DailyWorkRecordsApiService);
  private dayRecords: Record<string, AttendanceDayPage> | null = null;
  private periodRecords: Record<string, AttendanceRecord[]> | null = null;
  private version = 0;

  attendanceDayRecords(date: string, paging: LazyDTO) {
    return defer(() => {
      const key = JSON.stringify([date, paging.taken, paging.sectionSize]);
      const cached = this.dayRecords?.[key];
      if (cached !== undefined) return of(cached);
      const version = this.version;

      return this.api.attendanceDayRecords(date, paging).pipe(
        tap((records) => {
          if (version !== this.version) return;
          this.dayRecords ??= {};
          this.dayRecords[key] = records;
        }),
      );
    });
  }

  attendance(from: string, to: string, userId?: string) {
    return defer(() => {
      const key = JSON.stringify([from, to, userId ?? null]);
      const cached = this.periodRecords?.[key];
      if (cached !== undefined) return of(cached);
      const version = this.version;

      return this.api.attendance(from, to, userId).pipe(
        tap((records) => {
          if (version !== this.version) return;
          this.periodRecords ??= {};
          this.periodRecords[key] = records;
        }),
      );
    });
  }

  saveAttendance(data: Omit<AttendanceRecord, 'id'>, id?: string) {
    return this.api.saveAttendance(data, id).pipe(tap(() => this.reset()));
  }

  reset() {
    this.version++;
    this.dayRecords = null;
    this.periodRecords = null;
  }
}
