import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AttendanceEmployee } from '../../../../../core/dto/attendance/attendance-employee-dto';
import { AttendanceDaySave } from '../../attendance.model';
import { AttendanceEmployeeRowComponent } from '../attendance-employee-row/attendance-employee-row.component';
import { EmployeePaginationComponent } from '../../../employees/components/employee-pagination/employee-pagination.component';

@Component({
  selector: 'app-attendance-day-table',
  imports: [DatePipe, AttendanceEmployeeRowComponent, EmployeePaginationComponent],
  templateUrl: './attendance-day-table.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendanceDayTableComponent {
  readonly date = input.required<string>();
  readonly employees = input<AttendanceEmployee[]>([]);
  readonly total = input(0);
  readonly page = input(1);
  readonly pageSize = input(5);
  readonly loading = input(false);
  readonly savingUserId = input<string | null>(null);

  readonly saved = output<AttendanceDaySave>();
  readonly pageChanged = output<number>();
  readonly pageSizeChanged = output<number>();
}
