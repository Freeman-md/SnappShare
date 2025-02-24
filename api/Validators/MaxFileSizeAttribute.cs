using System.ComponentModel.DataAnnotations;

namespace api.Validators;

public class MaxFileSizeAttribute : ValidationAttribute
{
    private readonly long _maxFileSize;

    public MaxFileSizeAttribute(int maxFileSizeInMB)
    {
        _maxFileSize = maxFileSizeInMB * 1024 * 1024;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null) return ValidationResult.Success;

        if (value is IFormFile formFile && formFile.Length <= _maxFileSize)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage ?? $"File size must be less than {_maxFileSize / (1024 * 1024)}MB.");
    }
}
