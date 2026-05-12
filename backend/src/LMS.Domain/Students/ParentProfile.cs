using LMS.Domain.Common;
using LMS.Domain.Users;

namespace LMS.Domain.Students;

/// <summary>
/// Profile for a parent/guardian user.
///
/// Design notes:
/// - Thin aggregate: it only links a User identity to their student children.
/// - The parent's children are reached via StudentProfile.ParentId → ParentProfile.Id.
/// - Phase 3: introduce a ParentStudentLink join table to support M:M relationships
///   (step-parents, divorced families, multiple-guardian households).
/// - Phase 3: add notification preferences, language preference, contact details.
/// </summary>
public sealed class ParentProfile : AuditableEntity
{
    private ParentProfile() { }

    /// <summary>FK to the User aggregate that owns identity/auth for this parent.</summary>
    public Guid UserId { get; private set; }

    /// <summary>null = platform-wide; non-null = scoped to this tenant.</summary>
    public Guid? TenantId { get; private set; }

    // Reference navigation.
    public User User { get; private set; } = null!;

    public static ParentProfile Create(Guid userId, Guid? tenantId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId must not be empty.", nameof(userId));

        return new ParentProfile
        {
            UserId    = userId,
            TenantId  = tenantId,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
