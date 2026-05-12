namespace LMS.Application.Content;

/// <summary>
/// Thrown when a requested content entity does not exist, or when the requesting user
/// does not own it (tenant-scoped access).  Both cases surface as 404 so callers
/// cannot infer the existence of another tenant's data.
/// </summary>
public sealed class ContentNotFoundException : Exception
{
    public ContentNotFoundException(string entityType, Guid id)
        : base($"{entityType} with id '{id}' was not found.") { }

    public ContentNotFoundException(string message)
        : base(message) { }
}
