using System.ComponentModel.DataAnnotations;
using HR_System.Core.Enums;

namespace HR_System.Core.DTO.Payments;

public class PaymentQueryDTO
{
    [Range(1, 9999)]
    public required int Year { get; set; }

    [Range(1, 12)]
    public int? Month { get; set; }

    [EnumDataType(typeof(PaymentStateEnum))]
    public PaymentStateEnum? State { get; set; }

    [MaxLength(100)]
    public string? FullName { get; set; }

    public bool HistoryOnly { get; set; }

    public Guid? UserId { get; set; }

    [Range(0, int.MaxValue)]
    public int Taken { get; set; }

    [Range(1, int.MaxValue)]
    public int SectionSize { get; set; } = 100;
}
