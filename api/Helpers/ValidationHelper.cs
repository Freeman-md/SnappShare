using api.Enums;

namespace Helpers.ValidationHelper;

public static class ValidationHelper {
    public static void ValidateString(string? value, string name, string message = "must be provided.")
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} {message}", name);
    }

    public static void ValidatePositiveNumber(long value, string name)
    {
        if (value <= 0)
            throw new ArgumentException($"{name} must be a positive number.", name);
    }

    public static void ValidateNonNegativeNumber(int value, string name)
    {
        if (value < 0)
            throw new ArgumentException($"{name} must be a non-negative number.", name);
    }

    public static void ValidateChunkFile(IFormFile? file, string name = "chunkFile")
    {
        if (file == null || file.Length <= 0)
            throw new ArgumentException("Chunk file must not be null or empty.", name);
    }

    public static void ValidateExpiryDuration(ExpiryDuration value, string name)
    {
        if (!Enum.IsDefined(typeof(ExpiryDuration), value))
        {
            throw new ArgumentOutOfRangeException(name, $"Invalid expiry duration: {value}");
        }
    }
}