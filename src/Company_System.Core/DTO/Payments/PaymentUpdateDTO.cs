using System.ComponentModel.DataAnnotations;

namespace HR_System.Core.DTO.Payments;

public class PaymentUpdateDTO
{
    [Required]
    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public required decimal PaidAmount { get; set; }

    public DateOnly? PaymentDate { get; set; }
}
