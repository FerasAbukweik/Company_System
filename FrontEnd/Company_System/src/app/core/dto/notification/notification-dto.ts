export interface NotificationDTO {
  id: string;
  userId: string;
  title: string;
  content: string;
  targetPath: string;
  createdAt: string;
  isRead: boolean;
  type: number;
}
