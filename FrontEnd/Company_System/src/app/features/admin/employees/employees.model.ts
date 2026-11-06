import { PositionsEnum } from '../../../core/enum/positions-enum';

export interface EmployeeFormValue {
  fullName: string;
  userName: string;
  email: string;
  phone: string;
  position: PositionsEnum;
  hourlyRate: number | null;
  annualHolidayAllowance: number | null;
  password: string;
  parentId: string;
  image: File | null;
}

export type RowAction = 'hourlyRate' | 'profile' | 'terminate';
export const POSITION_OPTIONS = Object.values(PositionsEnum)
  .filter((value): value is PositionsEnum => typeof value === 'number')
  .map((value) => ({ value, label: String(PositionsEnum[value]) }));
export const ASSIGNABLE_POSITION_OPTIONS = POSITION_OPTIONS.filter(
  (option) => option.value !== PositionsEnum.unknown,
);
