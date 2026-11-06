using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Payments;

public class PaymentDTO
{
    public required Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public string? EmployeeName { get; set; }

    public string? ImageUrl { get; set; }

    public required int Year { get; set; }

    public required int Month { get; set; }

    public required decimal PaidAmount { get; set; }

    public DateOnly? PaymentDate { get; set; }

    public required PaymentStateEnum State { get; set; }
}
