namespace LMS.Application.Teacher;

/// <summary>
/// Thrown when a teacher attempts to access a student not assigned to them,
/// or when the teacher's user context cannot be resolved.
/// Maps to HTTP 403 Forbidden in <c>ExceptionHandlingMiddleware</c>.
/// </summary>
public sealed class TeacherAccessDeniedException : Exception
{
    public TeacherAccessDeniedException()
        : base("You do not have permission to access this resource.") { }

    public TeacherAccessDeniedException(Guid studentId)
        : base($"Student '{studentId}' is not assigned to you.") { }
}
