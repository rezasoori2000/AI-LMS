namespace LMS.Application.Student;

/// <summary>
/// Thrown when a student attempts to access content — a subject's chapters, or a specific
/// lesson — that is not within one of their active enrollments.
///
/// Maps to HTTP 403 Forbidden (handled by ExceptionHandlingMiddleware).
///
/// Why 403 and not 404:
///   Returning 404 would allow a student to enumerate subject and lesson IDs by probing
///   the API.  403 reveals only that access was denied, not whether the resource exists.
///
/// Distinct from a missing resource (ContentNotFoundException → 404):
///   The lesson or subject may exist, but the calling student is simply not authorised
///   to access it because they have no active enrollment in that subject.
/// </summary>
public sealed class StudentAccessDeniedException : Exception
{
    public StudentAccessDeniedException(string resourceType, Guid resourceId)
        : base($"You are not authorised to access {resourceType} '{resourceId}'.") { }
}
