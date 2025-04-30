using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace api.Middlewares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "An unhandled exception occurred.");

            var (statusCode, message) = exception switch
            {
                ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
                KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
                Azure.RequestFailedException azureEx => (StatusCodes.Status400BadRequest, ExtractAzureErrorMessage(azureEx)),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = "Error",
                Detail = message
            };

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
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
}
