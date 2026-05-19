using LMS.Domain.Common;
using LMS.Domain.Curriculum;
using LMS.Domain.Users;

namespace LMS.Domain.Students;

/// <summary>
/// Profile for a student user — extends the User identity record with
/// academic attributes (current grade, parent link, date of birth).
///
/// Design notes:
/// - UserId is a FK to the User aggregate; the profile never duplicates identity data
///   (email, password). Only student-specific state lives here.
/// - GradeId is nullable: a student may be created before a grade is assigned by admin.
/// - ParentId is nullable: a student may not have a linked parent on this platform
///   (home-school, adult learner, orphaned account, etc.).
/// - Phase 1: one student ↔ one parent (1:1 via ParentId FK).
///   Phase 3: introduce a ParentStudentLink join table for M:M (step-parents,
///   divorced families, shared custody, multiple guardian scenarios).
/// - Teacher linkage uses M:M via <see cref="TeacherStudentAssignment"/>.
///   Phase 1 admin convention: at most one teacher per student (enforced in application layer).
///   Phase 3: co-teaching, subject-specific teachers, intervention ownership.
/// </summary>
public sealed class StudentProfile : AuditableEntity
{
    private StudentProfile() { }

    /// <summary>FK to the User aggregate that owns identity/auth for this student.</summary>
    public Guid UserId { get; private set; }

    /// <summary>FK to the student's current grade. Nullable until assigned by admin.</summary>
    public Guid? GradeId { get; private set; }

    /// <summary>
    /// FK to the linked ParentProfile. Nullable (parent linkage is optional).
    /// Phase 3: remove in favour of ParentStudentLink join table.
    /// </summary>
    public Guid? ParentId { get; private set; }

    public DateOnly? DateOfBirth { get; private set; }

    /// <summary>null = platform-wide (SuperAdmin-created demo student); non-null = tenant-scoped.</summary>
    public Guid? TenantId { get; private set; }

    // Reference navigation — EF Core populates on explicit Include().
    public User           User   { get; private set; } = null!;
    public Grade?         Grade  { get; private set; }
    public ParentProfile? Parent { get; private set; }

    /// <summary>Teacher assignments for this student. Phase 1: at most one entry by convention.</summary>
    public ICollection<TeacherStudentAssignment> TeacherAssignments { get; private set; } = [];

    public static StudentProfile Create(
        Guid      userId,
        Guid?     gradeId     = null,
        Guid?     parentId    = null,
        DateOnly? dateOfBirth = null,
        Guid?     tenantId    = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId must not be empty.", nameof(userId));

        return new StudentProfile
        {
            UserId      = userId,
            GradeId     = gradeId,
            ParentId    = parentId,
            DateOfBirth = dateOfBirth,
            TenantId    = tenantId,
            CreatedAt   = DateTime.UtcNow,
        };
    }

    /// <summary>Assigns (or reassigns) the student to a grade.</summary>
    public void AssignGrade(Guid gradeId)
    {
        if (gradeId == Guid.Empty)
            throw new ArgumentException("GradeId must not be empty.", nameof(gradeId));

        GradeId   = gradeId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Links this student to a parent profile.</summary>
    public void LinkParent(Guid parentId)
    {
        if (parentId == Guid.Empty)
            throw new ArgumentException("ParentId must not be empty.", nameof(parentId));

        ParentId  = parentId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes any existing parent link for this student.
    /// Safe to call when already unlinked — no-op.
    /// </summary>
    public void UnlinkParent()
    {
        ParentId  = null;
        UpdatedAt = DateTime.UtcNow;
    }

}
