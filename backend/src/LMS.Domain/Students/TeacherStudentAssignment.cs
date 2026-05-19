using LMS.Domain.Common;
using LMS.Domain.Users;

namespace LMS.Domain.Students;

/// <summary>
/// Represents an explicit assignment between a teacher (User with Role = Teacher)
/// and a student (StudentProfile).
///
/// Design notes:
/// - Phase 1: one student has at most one teacher by admin convention. The schema supports
///   many-to-many so Phase 3 can introduce co-teaching or subject-specific assignments
///   without a breaking migration.
/// - <see cref="AssignedByUserId"/> records the admin who made the assignment for auditing.
/// - Tenancy is inherited implicitly via the student's TenantId; no redundant column needed.
/// - The composite unique index (TeacherUserId, StudentProfileId) prevents duplicate rows
///   but is relaxed automatically when Phase 3 lifts the one-teacher-per-student convention.
/// </summary>
public sealed class TeacherStudentAssignment : Entity
{
    private TeacherStudentAssignment() { }

    /// <summary>
    /// Creates a new teacher–student assignment.
    /// </summary>
    /// <param name="teacherUserId">The <see cref="User.Id"/> of the assigning teacher.</param>
    /// <param name="studentProfileId">The <see cref="StudentProfile.Id"/> of the student.</param>
    /// <param name="assignedByUserId">The <see cref="User.Id"/> of the admin performing the assignment.</param>
    public static TeacherStudentAssignment Create(
        Guid teacherUserId,
        Guid studentProfileId,
        Guid assignedByUserId)
    {
        if (teacherUserId == Guid.Empty)
            throw new ArgumentException("TeacherUserId must not be empty.", nameof(teacherUserId));

        if (studentProfileId == Guid.Empty)
            throw new ArgumentException("StudentProfileId must not be empty.", nameof(studentProfileId));

        return new TeacherStudentAssignment
        {
            TeacherUserId    = teacherUserId,
            StudentProfileId = studentProfileId,
            AssignedAt       = DateTime.UtcNow,
            AssignedByUserId = assignedByUserId,
        };
    }

    /// <summary>FK to <see cref="User.Id"/> — must have Role = Teacher.</summary>
    public Guid TeacherUserId { get; private set; }

    /// <summary>FK to <see cref="StudentProfile.Id"/>.</summary>
    public Guid StudentProfileId { get; private set; }

    /// <summary>UTC timestamp when the assignment was recorded.</summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>FK to <see cref="User.Id"/> of the admin who created this assignment.</summary>
    public Guid AssignedByUserId { get; private set; }

    // ── Navigations ────────────────────────────────────────────────────────

    /// <summary>Loaded when included; null otherwise.</summary>
    public User? Teacher { get; private set; }

    /// <summary>Loaded when included; null otherwise.</summary>
    public StudentProfile? Student { get; private set; }
}
