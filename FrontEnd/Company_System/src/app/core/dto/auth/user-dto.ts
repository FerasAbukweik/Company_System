export interface UserDTO {
  userId: string;
  userName: string;
  fullName: string;
  email: string | null;
  phone: string | null;
  position: string;
  userImageUrl: string;
  hourlyRate: number | null;
  annualHolidayAllowance: number | null;
  isActive: boolean;
}
