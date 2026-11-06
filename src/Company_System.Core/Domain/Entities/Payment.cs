using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Payments;
using HR_System.Core.Enums;

namespace HR_System.Core.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Range(1, 9999)]
    public required int Year { get; set; }

    [Required]
    [Range(1, 12)]
    public required int Month { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    [Column(TypeName = "decimal(18,2)")]
    public required decimal PaidAmount { get; set; }

    public DateOnly? PaymentDate { get; set; }


    [EnumDataType(typeof(PaymentStateEnum))]
    public PaymentStateEnum State { get; set; } = PaymentStateEnum.Pending;


    // relations

    [Required]
    public required Guid UserId { get; set; }

    public ApplicationUser? User { get; set; }


    // functions

    public PaymentDTO ToDTO()
    {
        return new PaymentDTO()
        {
            Id = Id,
            UserId = UserId,
            EmployeeName = User?.FullName,
            ImageUrl = User?.Image?.Url,
            Year = Year,
            Month = Month,
            PaidAmount = PaidAmount,
            PaymentDate = PaymentDate,
            State = State
        };
    }
}
