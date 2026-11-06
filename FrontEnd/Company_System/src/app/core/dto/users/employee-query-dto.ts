import { LazyDTO } from '../lazy-loading/lazy-dto';

export interface EmployeeQuery extends LazyDTO {
  search?: string;
  activeOnly?: boolean;
}
