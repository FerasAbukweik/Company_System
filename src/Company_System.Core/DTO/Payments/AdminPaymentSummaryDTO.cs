namespace HR_System.Core.DTO.Payments;

public class AdminPaymentSummaryDTO
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal RejectedAmount { get; set; }
}
