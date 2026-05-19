using LMS.Application.Auth;
using LMS.Application.Content;
using LMS.Application.Parent;
using LMS.Application.Student;
using LMS.Application.Teacher;

namespace LMS.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns a consistent RFC 7807 Problem Details response.
/// This must be the outermost middleware in the pipeline.
///
/// Mapped exceptions:
/// <list type="bullet">
///   <item><see cref="InvalidCredentialsException"/> → 401 Unauthorized</item>
///   <item><see cref="EmailAlreadyRegisteredException"/> → 409 Conflict</item>
///   <item><see cref="ContentNotFoundException"/> → 404 Not Found</item>
///   <item><see cref="ContentConflictException"/> → 409 Conflict</item>
///   <item><see cref="ParentAccessDeniedException"/> → 403 Forbidden</item>
///   <item><see cref="StudentAccessDeniedException"/> → 403 Forbidden</item>
///   <item><see cref="LessonAlreadyCompletedException"/> → 409 Conflict</item>
///   <item>Anything else → 500 Internal Server Error</item>
/// </list>
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
            // Log at Warning for expected domain errors; Error for genuine server faults.
            if (ex is InvalidCredentialsException
                   or EmailAlreadyRegisteredException
                   or ContentNotFoundException
                   or ContentConflictException
                   or ParentAccessDeniedException
                   or StudentAccessDeniedException
                   or LessonAlreadyCompletedException
                   or TeacherAccessDeniedException)
                _logger.LogWarning(
                    "Domain exception. Method={Method} Path={Path} Type={ExType} TraceId={TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.GetType().Name,
                    context.TraceIdentifier);
            else
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

        var (status, type, title) = exception switch
        {
            InvalidCredentialsException =>
                (StatusCodes.Status401Unauthorized,
                 "https://tools.ietf.org/html/rfc7235#section-3.1",
                 "Authentication failed."),

            EmailAlreadyRegisteredException =>
                (StatusCodes.Status409Conflict,
                 "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                 "The email address is already registered."),

            ContentNotFoundException =>
                (StatusCodes.Status404NotFound,
                 "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                 exception.Message),

            ContentConflictException =>
                (StatusCodes.Status409Conflict,
                 "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                 exception.Message),

            ParentAccessDeniedException =>
                (StatusCodes.Status403Forbidden,
                 "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                 exception.Message),

            StudentAccessDeniedException =>
                (StatusCodes.Status403Forbidden,
                 "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                 exception.Message),

            LessonAlreadyCompletedException =>
                (StatusCodes.Status409Conflict,
                 "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                 exception.Message),

            TeacherAccessDeniedException =>
                (StatusCodes.Status403Forbidden,
                 "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                 exception.Message),

            _ =>
                (StatusCodes.Status500InternalServerError,
                 "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                 "An unexpected error occurred.")
        };

        context.Response.StatusCode = status;

        var problem = new
        {
            type,
            title,
            status,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
