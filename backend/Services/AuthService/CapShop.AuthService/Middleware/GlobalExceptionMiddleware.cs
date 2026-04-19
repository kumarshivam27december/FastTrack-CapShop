using System.Text.Json;

namespace CapShop.AuthService.Middleware
{
    public class GlobalExceptionMiddleware
    {
        // dependency injection of RequestDelegate to call the next middleware in the pipeline and ILogger to log unhandled exceptions with trace identifiers for better debugging and monitoring of errors in the application
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        // This method is invoked for each HTTP request and wraps the execution of the next middleware in a try-catch block to handle specific exceptions like UnauthorizedAccessException and InvalidOperationException with appropriate status codes and messages, while also catching any unhandled exceptions to log them with a trace identifier and return a generic error response to the client without exposing sensitive details about the error

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                var status = ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;

                await WriteErrorAsync(context, status, ex.Message);
            }
            catch (Exception ex)
            {
                var traceId = context.TraceIdentifier;
                _logger.LogError(ex, "Unhandled exception. TraceId: {TraceId}", traceId);
                await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.", traceId);
            }
        }

        private static async Task WriteErrorAsync(HttpContext context, int status, string message, string? traceId = null)
        {
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";

            object payload = traceId is null ? new { message } : new { message, traceId };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}