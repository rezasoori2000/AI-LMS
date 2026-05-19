namespace LMS.Application.Student;

/// <summary>
/// Thrown when a student attempts to submit completion for a lesson that has
/// already been completed.
///
/// Maps to HTTP 409 Conflict (handled by ExceptionHandlingMiddleware).
///
/// Phase 1: one attempt per lesson is the enforced rule.
/// Phase 3: when re-attempt support is added, this exception will be removed or
/// replaced with a richer "attempt limit exceeded" concept.
/// </summary>
public sealed class LessonAlreadyCompletedException : Exception
{
    public LessonAlreadyCompletedException(Guid lessonId)
        : base($"Lesson '{lessonId}' has already been completed and cannot be submitted again.") { }
}
