import { TaskPrioritiesEnum } from '../enum/priorities-enum';
import { TaskStatusEnum } from '../enum/task-states-enum';

export interface TaskDTO {
  id: string;
  title: string;
  description: string;
  created: Date;
  deadline: Date;
  priority: TaskPrioritiesEnum;
  status: TaskStatusEnum;
  userId: string;
  managerId: string;
}
