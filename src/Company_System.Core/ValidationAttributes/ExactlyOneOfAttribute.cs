using System.ComponentModel.DataAnnotations;

namespace HR_System.Core.ValidationAttributes;

/// <summary>
/// Validates that exactly one of two properties has a value:
/// both null (empty) fails, and both filled fails.
/// Put it on either of the two properties and pass the name of the other one.
/// "No value" means null, a blank string, or Guid.Empty.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ExactlyOneOfAttribute(string otherPropertyName) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var otherProperty = validationContext.ObjectType.GetProperty(otherPropertyName);
        if (otherProperty is null)
            return new ValidationResult($"{otherPropertyName} name wasn't found");

        var otherValue = otherProperty.GetValue(validationContext.ObjectInstance);

        var hasThis = HasValue(value);
        var hasOther = HasValue(otherValue);

        if (hasThis == hasOther)
        {
            var thisName = validationContext.MemberName ?? validationContext.DisplayName;
            var message = ErrorMessage ?? (hasThis
                ? $"{thisName} and {otherPropertyName} cannot both have a value, provide only one."
                : $"One of {thisName} or {otherPropertyName} is required.");

            // report the error on both properties
            return new ValidationResult(message, [thisName, otherPropertyName]);
        }

        return ValidationResult.Success;
    }

    private static bool HasValue(object? value) => value switch
    {
        null => false,
        string s => !string.IsNullOrWhiteSpace(s),
        Guid g => g != Guid.Empty,
        _ => true
    };
}