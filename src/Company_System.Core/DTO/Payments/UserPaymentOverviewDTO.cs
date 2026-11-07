namespace HR_System.Core.DTO.Payments;

public class UserPaymentOverviewDTO
{
    public decimal ApprovedAmount { get; set; }

    public decimal PendingAmount { get; set; }

    public int PaidLeaveDays { get; set; }

    public PaymentDTO? LatestApproved { get; set; }
}
