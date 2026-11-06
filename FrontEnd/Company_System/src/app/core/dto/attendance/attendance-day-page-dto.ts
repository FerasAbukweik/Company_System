import { AttendanceEmployee } from './attendance-employee-dto';

export interface AttendanceDayPage {
  items: AttendanceEmployee[];
  totalCount: number;
}
