import { NotificationDTO } from './notification-dto';

export interface NotificationPageDTO {
  items: NotificationDTO[];
  unreadCount: number;
}
