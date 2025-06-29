using System.Net;
using System.Text.Json;

namespace v2
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse();

            switch (exception)
            {
                case Microsoft.AspNetCore.Http.BadHttpRequestException ex when ex.Message.Contains("Unexpected end of request content"):
                    errorResponse.Message = "Request content incomplete";
                    errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    _logger.LogWarning("Incomplete request: {Message}", ex.Message);
                    break;

                case OperationCanceledException ex when !(ex is TaskCanceledException):
                    errorResponse.Message = "Request was cancelled";
                    errorResponse.StatusCode = 499; // Client Closed Request
                    response.StatusCode = 499;
                    _logger.LogInformation("Request cancelled: {Message}", exception.Message);
                    break;

                case TaskCanceledException ex when ex.InnerException is TimeoutException:
                    errorResponse.Message = "Request timeout";
                    errorResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    _logger.LogWarning("Request timeout: {Message}", exception.Message);
                    break;

                case TaskCanceledException:
                    errorResponse.Message = "Request was cancelled";
                    errorResponse.StatusCode = 499; // Client Closed Request
                    response.StatusCode = 499;
                    _logger.LogInformation("Task cancelled: {Message}", exception.Message);
                    break;

                case System.Text.Json.JsonException:
                    errorResponse.Message = "Invalid JSON format";
                    errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    _logger.LogWarning("JSON parsing error: {Message}", exception.Message);
                    break;

                default:
                    errorResponse.Message = "An internal server error occurred";
                    errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(errorResponse);
            await response.WriteAsync(jsonResponse);
        }

        public class ErrorResponse
        {
            public string Message { get; set; } = string.Empty;
            public int StatusCode { get; set; }
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        }
    }
}
