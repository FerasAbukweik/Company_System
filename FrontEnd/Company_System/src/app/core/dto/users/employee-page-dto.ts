import { EmployeeDTO } from './employee-dto';

export interface EmployeePage {
  items: EmployeeDTO[];
  totalCount: number;
}
