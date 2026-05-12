namespace LMS.Application.Parent;

/// <summary>
/// Thrown when a parent attempts to access a student record that is not linked to them.
/// Maps to HTTP 403 Forbidden so callers cannot enumerate other parents' children.
///
/// Distinct from ContentNotFoundException (404) — the requested studentId may be valid
/// but simply not linked to the calling parent.  403 is correct: the resource exists,
/// the caller is authenticated, but is not authorised to access it.
/// </summary>
public sealed class ParentAccessDeniedException : Exception
{
    public ParentAccessDeniedException(Guid studentId)
        : base($"You are not authorised to access student '{studentId}'.") { }
}
