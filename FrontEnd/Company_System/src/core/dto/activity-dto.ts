import { ActivityTypeEnum } from "../enums/activity-type-enum";

export interface ActivityDTO {
  id: string;
  type: ActivityTypeEnum;
  createdAt: string;
  title: string;
  description: string;
  name: string | null;
}