import { TaskPrioritiesEnum } from "../enum/priorities-enum";

export interface TaskAddDTO {
  title: string;
  description: string;
  deadline: Date;
  priority: TaskPrioritiesEnum;
  userId: string; // Guid in C# is typically represented as a string (UUID) in TypeScript
}
