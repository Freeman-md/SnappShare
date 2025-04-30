

using api.Models;

namespace api.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            Azure.RequestFailedException azureEx => (StatusCodes.Status400BadRequest, ExtractAzureErrorMessage(azureEx)),
            _ => (StatusCodes.Status400BadRequest, "An unexpected error occurred.")
        };

        var response = new ErrorApiResponse<object>(message);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsJsonAsync(response);
    }

    private string ExtractAzureErrorMessage(Azure.RequestFailedException ex)
    {
        return ex.ErrorCode switch
        {
            "BlobNotFound" => "The specified blob does not exist.",
            "AuthorizationPermissionMismatch" => "You don't have permission to upload this file.",
            "ContainerNotFound" => "The specified blob container does not exist.",
            _ => "An unexpected Azure storage error occurred."
        };
    }
}
