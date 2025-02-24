namespace api.Models;

public class SuccessApiResponse<T> : ApiResponse<T> {
    public SuccessApiResponse(int statusCode = 200, string message = "Success") {
        StatusCode = statusCode;
        Message = message;
    }
}