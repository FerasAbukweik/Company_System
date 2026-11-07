namespace HR_System.Core.DTO.Payments;

public class AdminPaymentPageDTO
{
    public required IReadOnlyList<PaymentDTO> Items { get; set; }

    public required int TotalCount { get; set; }

}
