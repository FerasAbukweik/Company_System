import { EmployeeDTO } from './employee-dto';
import { EmployeesSummary } from './employees-summary-dto';

export interface EmployeePage {
  items: EmployeeDTO[];
  totalCount: number;
  summary: EmployeesSummary;
}
