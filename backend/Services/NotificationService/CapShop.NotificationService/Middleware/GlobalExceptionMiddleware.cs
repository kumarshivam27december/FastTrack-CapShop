using System.Text.Json;

namespace CapShop.NotificationService.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

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
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
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

        object payload = traceId is null
            ? new { message }
            : new { message, traceId };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
