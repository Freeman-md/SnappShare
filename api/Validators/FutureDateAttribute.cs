using System.ComponentModel.DataAnnotations;

namespace api.Validators;

public class FutureDateAttribute : ValidationAttribute {
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateTimeOffset expiryTime && expiryTime > DateTimeOffset.UtcNow) {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage ?? "Expiry time must be in the future.");
    }
}