using System.ComponentModel.DataAnnotations;

namespace api.Validators;

public class AllowedFileExtensionsAttribute : ValidationAttribute {
    private static readonly HashSet<string> AllowedExtensions = new HashSet<string>{ ".jpg", ".png", ".pdf", ".txt" };

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext) {
        if (value is IFormFile formFile) {
            var extension = Path.GetExtension(formFile.FileName).ToLower();

            if (AllowedExtensions.Contains(extension)) {
                return ValidationResult.Success!;
            }
        }

        return new ValidationResult(ErrorMessage ?? "Invalid file type. Allowed types: .jpg, .png, .pdf, .txt");
    }

}