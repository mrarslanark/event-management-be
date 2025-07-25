using EventManagement.Exceptions;
using FluentValidation;

namespace EventManagement.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            context.Response.ContentType = "application/json";

            if (ex is ValidationException validationException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .Select(g => new
                    {
                        field = g.Key.ToLower(),
                        error = g.Select(e => e.ErrorMessage).FirstOrDefault()
                    });

                var validationResponse = new
                {
                    createdAt = DateTime.UtcNow,
                    status = context.Response.StatusCode,
                    error = errors,
                };

                await context.Response.WriteAsJsonAsync(validationResponse);
                return;
            }

            context.Response.StatusCode = ex switch
            {
                ConflictException => StatusCodes.Status409Conflict,
                ArgumentException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError,
            };

            var response = new Dictionary<string, object?>
            {
                ["createdAt"] =  DateTime.UtcNow,
                ["error"] = ex.Message,
                ["status"] = context.Response.StatusCode
            };

            if (!_env.IsProduction())
            {
                response["stacktrace"] = ex.StackTrace;
            }

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}