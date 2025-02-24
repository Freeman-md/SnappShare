using System.ComponentModel.DataAnnotations;
using api.Models;

namespace api.Validators;

public class FileOrNoteRequiredAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var fileUpload = (FileUpload)validationContext.ObjectInstance;

        bool hasFile = fileUpload.File != null;
        bool hasNote = !string.IsNullOrWhiteSpace(fileUpload.Note);

        if (!hasFile && !hasNote)
        {
            return new ValidationResult("Either a file or a note must be provided.", new[] { nameof(fileUpload.File) });
        }

        return ValidationResult.Success;
    }
}
