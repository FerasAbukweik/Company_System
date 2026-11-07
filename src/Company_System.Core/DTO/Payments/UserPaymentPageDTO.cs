namespace HR_System.Core.DTO.Payments;

public class UserPaymentPageDTO
{
    public required IReadOnlyList<PaymentDTO> Items { get; set; }

    public int TotalCount { get; set; }
}
