using LMS.Domain.Common;
using LMS.Domain.Curriculum;
using LMS.Domain.Students;

namespace LMS.Domain.Enrollments;

/// <summary>
/// Records a student's enrollment in a subject.
///
/// Design notes:
/// - The (StudentId, SubjectId) pair must be unique — unique index enforced in Part 2.
/// - Completed and Dropped are terminal states. Re-enrollment creates a new Enrollment record.
/// - EnrolledAt is set to UtcNow on creation; it is never updated.
/// - Phase 3: add StartDate, EndDate, EnrolledBy (admin Guid), notes, grade override,
///   and a class/cohort assignment.
/// </summary>
public sealed class Enrollment : AuditableEntity
{
    private Enrollment() { }

    /// <summary>FK to the StudentProfile this enrollment belongs to.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>FK to the Subject the student is enrolled in.</summary>
    public Guid SubjectId { get; private set; }

    public EnrollmentStatus Status { get; private set; }

    /// <summary>UTC timestamp when the enrollment record was created.</summary>
    public DateTime EnrolledAt { get; private set; }

    /// <summary>Set when Status transitions to Completed.</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>null = platform-wide; non-null = scoped to this tenant.</summary>
    public Guid? TenantId { get; private set; }

    // Reference navigation.
    public StudentProfile Student { get; private set; } = null!;
    public Subject        Subject { get; private set; } = null!;

    public static Enrollment Create(Guid studentId, Guid subjectId, Guid? tenantId = null)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("StudentId must not be empty.", nameof(studentId));
        if (subjectId == Guid.Empty)
            throw new ArgumentException("SubjectId must not be empty.", nameof(subjectId));

        return new Enrollment
        {
            StudentId  = studentId,
            SubjectId  = subjectId,
            Status     = EnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow,
            TenantId   = tenantId,
            CreatedAt  = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Transitions the enrollment to Completed.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when not in Active state.</exception>
    public void Complete()
    {
        if (Status != EnrollmentStatus.Active)
            throw new InvalidOperationException(
                $"Only an Active enrollment can be completed (current status: {Status}).");

        Status      = EnrollmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt   = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the enrollment to Dropped.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when not in Active state.</exception>
    public void Drop()
    {
        if (Status != EnrollmentStatus.Active)
            throw new InvalidOperationException(
                $"Only an Active enrollment can be dropped (current status: {Status}).");

        Status    = EnrollmentStatus.Dropped;
        UpdatedAt = DateTime.UtcNow;
    }
}
