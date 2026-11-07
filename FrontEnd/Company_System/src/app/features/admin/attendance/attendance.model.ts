export interface AttendanceDaySave {
  userId: string;
  clockIn: string | null;
  clockOut: string | null;
  state: number;
  recordId?: string;
}
