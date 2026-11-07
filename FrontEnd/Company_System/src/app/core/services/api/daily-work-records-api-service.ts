import { LazyDTO } from '../../dto/lazy-loading/lazy-dto';
import { AttendanceDayPage } from '../../dto/attendance/attendance-day-page-dto';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Urls } from '../../constants/urls';
import { AttendanceRecord } from '../../dto/attendance/attendance-record-dto';

@Injectable({ providedIn: 'root' })
export class DailyWorkRecordsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = Urls.api + '/DailyWorkRecords';

  attendanceDayRecords(date: string, paging: LazyDTO) {
    return this.http.get<AttendanceDayPage>(this.base + '/GetDayRecords', {
      params: { date, ...paging },
    });
  }

  attendance(from: string, to: string, userId?: string) {
    return this.http.get<AttendanceRecord[]>(
      this.base + (userId ? '/GetUserRecords/' + userId : ''),
      { params: { from, to } },
    );
  }

  saveAttendance(data: Omit<AttendanceRecord, 'id'>, id?: string) {
    return id
      ? this.http.put<AttendanceRecord>(this.base + '/Update/' + id, {
          clockIn: data.clockIn,
          clockOut: data.clockOut,
          state: data.state,
        })
      : this.http.post<AttendanceRecord>(this.base + '/Add', data);
  }
}
