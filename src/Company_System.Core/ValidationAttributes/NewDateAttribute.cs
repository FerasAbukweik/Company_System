
using System.ComponentModel.DataAnnotations;

namespace HR_System.Core.ValidationAttributes;

public class NewDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;
        
        if (value is not DateTime date)
            return new ValidationResult(string.Format("{0} must be a valid DateTime.", validationContext.DisplayName));
        
        var utcDate = date.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(date, DateTimeKind.Utc) 
            : date.ToUniversalTime();
        
        if(utcDate <= DateTime.UtcNow)
            return new ValidationResult(string.Format("{0} have old Date.", validationContext.DisplayName));
        
        return  ValidationResult.Success;
    }
}