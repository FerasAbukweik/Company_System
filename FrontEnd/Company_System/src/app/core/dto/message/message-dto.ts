export interface MessageDTO {
  id: string;
  senderId: string;
  content: string;
  createdAt: Date;
  isCurrUserSender: boolean;
  groupName: string;
}
