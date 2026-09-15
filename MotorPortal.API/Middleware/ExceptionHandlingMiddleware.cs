using System.Net;
using System.Text.Json;
using MotorPortal.Application.Exceptions;

namespace MotorPortal.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            var traceId = context.TraceIdentifier;
            var (statusCode, message) = MapException(ex);

            _logger.LogError(ex, "Unhandled exception. TraceId: {TraceId}", traceId);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var payload = new
            {
                statusCode,
                message,
                traceId
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }

    private static (int StatusCode, string Message) MapException(Exception ex)
    {
        return ex switch
        {
            NotFoundException => ((int)HttpStatusCode.NotFound, ex.Message),
            BusinessRuleException => ((int)HttpStatusCode.BadRequest, ex.Message),
            ArgumentException => ((int)HttpStatusCode.BadRequest, ex.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };
    }
}
