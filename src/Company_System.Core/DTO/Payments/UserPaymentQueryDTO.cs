using System.ComponentModel.DataAnnotations;
using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Payments;

public class UserPaymentQueryDTO
{
    [Range(1, 9999)]
    public required int Year { get; set; }

    [EnumDataType(typeof(PaymentStateEnum))]
    public PaymentStateEnum? State { get; set; }

    [Range(0, int.MaxValue)]
    public int Taken { get; set; }

    [Range(1, int.MaxValue)]
    public int SectionSize { get; set; } = 5;
}
