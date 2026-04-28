namespace LMS.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns a consistent RFC 7807 Problem Details response.
/// This must be the outermost middleware in the pipeline.
/// </summary>
public sealed class ExceptionHandlingMiddleware
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
            _logger.LogError(
                ex,
                "Unhandled exception. Method={Method} Path={Path} TraceId={TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            await WriteErrorResponseAsync(context, ex);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // Do NOT leak exception details (message, stack trace) outside of development.
        // Differentiate via IWebHostEnvironment if you want dev-only detail (Phase 2+).
        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "An unexpected error occurred.",
            status = StatusCodes.Status500InternalServerError,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
