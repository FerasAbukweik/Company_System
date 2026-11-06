import { PositionsEnum } from '../../enum/positions-enum';

export interface EmployeeDTO {
  id: string;
  treeId?: string | null;
  parentId?: string | null;
  fullName: string;
  userName: string;
  email: string;
  phone: string;
  imageUrl: string | null;
  position: PositionsEnum;
  hourlyRate: number | null;
  annualHolidayAllowance: number | null;
  isActive: boolean;
}
