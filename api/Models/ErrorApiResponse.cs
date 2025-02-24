namespace api.Models;

public class ErrorApiResponse<T> : ApiResponse<T> {
    public string? ErrorMessage { get; set; }

    public ErrorApiResponse(string errorMessage, int statusCode = 400, string message = "Error") {
        StatusCode = statusCode;
        Message = message;
        ErrorMessage = errorMessage;
    }
}
