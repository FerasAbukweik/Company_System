import { ApprovalStatusEnum } from "../enums/approval-state-enum";

export interface RequestedApprovalDTO {
  id: string;
  createdOn: string;
  requesterName: string;
  body: string;
  status: ApprovalStatusEnum;
}