export interface MessageDTO {
  id: string;
  content: string;
  createdAt: Date;
  isCurrUserSender: boolean;
  groupName: string;
}
