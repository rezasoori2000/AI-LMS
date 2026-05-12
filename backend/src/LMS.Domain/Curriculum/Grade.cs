using LMS.Domain.Common;

namespace LMS.Domain.Curriculum;

/// <summary>
/// Represents an academic grade level (e.g., "Grade 1", "Year 7").
///
/// Design notes:
/// - TenantId null  → platform-wide reference grade (used across all tenants).
/// - TenantId set   → tenant-specific grade override (custom naming, non-standard systems).
/// - Grade is a reference entity: it has no behaviour beyond creation.
/// - Phase 3: add curriculum-standard mapping, grade-specific settings, max enrolment.
/// </summary>
public sealed class Grade : AuditableEntity
{
    // Required for EF Core materialisation.
    private Grade() { }

    /// <summary>Display name, e.g. "Grade 1" or "Year 7".</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Positive numeric level for ordering (typically 1–12 for K-12).</summary>
    public int Level { get; private set; }

    /// <summary>null = platform-wide; non-null = scoped to this tenant.</summary>
    public Guid? TenantId { get; private set; }

    public static Grade Create(string name, int level, Guid? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (level < 1)
            throw new ArgumentOutOfRangeException(nameof(level), "Grade level must be a positive integer.");

        return new Grade
        {
            Name      = name.Trim(),
            Level     = level,
            TenantId  = tenantId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void Update(string name, int level)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (level < 1)
            throw new ArgumentOutOfRangeException(nameof(level), "Grade level must be a positive integer.");

        Name      = name.Trim();
        Level     = level;
        UpdatedAt = DateTime.UtcNow;
    }
}
