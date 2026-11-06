export interface PaymentSummary {
  totalCount: number;
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
  pendingAmount: number;
  approvedAmount: number;
  rejectedAmount: number;
}
