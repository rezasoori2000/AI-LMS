namespace LMS.Application.Content;

/// <summary>
/// Thrown when a content mutation would violate a uniqueness constraint
/// (e.g. duplicate grade level, duplicate subject slug, duplicate chapter order
/// within the same Subject+Grade combination).
/// Mapped to HTTP 409 Conflict by <c>ExceptionHandlingMiddleware</c>.
/// </summary>
public sealed class ContentConflictException : Exception
{
    public ContentConflictException(string message) : base(message) { }
}
