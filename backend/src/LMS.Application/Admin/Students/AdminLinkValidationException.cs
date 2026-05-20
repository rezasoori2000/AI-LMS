namespace LMS.Application.Admin.Students;

/// <summary>
/// Thrown when an admin linkage operation references a resource (parent profile,
/// teacher user) that does not exist or is ineligible.
///
/// Maps to HTTP 400 Bad Request in <see cref="LMS.Api.Middleware.ExceptionHandlingMiddleware"/>.
///
/// Semantics: the request itself is malformed because it references a non-existent
/// foreign key in the request body — distinct from ContentNotFoundException (404)
/// which means the resource identified by the URL path does not exist.
/// </summary>
public sealed class AdminLinkValidationException : Exception
{
    public AdminLinkValidationException(string message) : base(message) { }
}
