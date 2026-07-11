import { PositionsEnum } from '../enum/positions-enum';

export interface AddEmployeeDTO {
  email: string;
  password: string;
  userName: string;
  fullName: string;
  phoneNumber: string;
  position: PositionsEnum;
  parentId: string;
  image: File;
}
