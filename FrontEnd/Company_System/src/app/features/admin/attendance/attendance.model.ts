export interface AttendanceDaySave {
  userId: string;
  minutesWorked: number;
  state: number;
  recordId?: string;
}
