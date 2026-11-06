import { AttendanceRecord } from './attendance-record-dto';

export interface AttendanceEmployee {
  userId: string;
  fullName: string;
  record: AttendanceRecord | null;
}
