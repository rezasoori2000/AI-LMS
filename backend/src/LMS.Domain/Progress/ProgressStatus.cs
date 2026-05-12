namespace LMS.Domain.Progress;

/// <summary>
/// Completion state for a student's progress through a single lesson.
/// Phase 3: add RequiresReview (waiting for teacher sign-off) state.
/// </summary>
public enum ProgressStatus
{
    NotStarted,
    InProgress,
    Completed,
}
