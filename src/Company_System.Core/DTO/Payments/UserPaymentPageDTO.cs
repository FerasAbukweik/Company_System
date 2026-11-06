namespace HR_System.Core.DTO.Payments;

public class UserPaymentPageDTO
{
    public required IReadOnlyList<PaymentDTO> Items { get; set; }

    public required UserPaymentSummaryDTO Summary { get; set; }

    public PaymentDTO? LatestApproved { get; set; }
}

public class UserPaymentSummaryDTO
{
    public int TotalCount { get; set; }

    public decimal ApprovedAmount { get; set; }

    public decimal PendingAmount { get; set; }
}
