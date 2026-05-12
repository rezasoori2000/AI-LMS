namespace LMS.Domain.Enrollments;

/// <summary>
/// Lifecycle state of a student's enrollment in a subject.
/// Completed and Dropped are terminal states — re-enrollment creates a new record.
/// Phase 3: add Suspended (temporary hold) and Waitlisted states.
/// </summary>
public enum EnrollmentStatus
{
    Active,
    Completed,
    Dropped,
}
