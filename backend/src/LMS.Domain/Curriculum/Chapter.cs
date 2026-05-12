using LMS.Domain.Common;

namespace LMS.Domain.Curriculum;

/// <summary>
/// A chapter groups a set of lessons within a specific subject at a specific grade level.
///
/// Design notes:
/// - A chapter is intentionally tied to BOTH a Subject and a Grade because the same
///   subject has different chapter content at different grade levels.
/// - Order determines the display sequence within a Subject+Grade combination.
/// - Navigation properties (Subject, Grade) support EF Core Include() queries.
/// - Phase 3: add IsPublished lifecycle, prerequisite chapter IDs, estimated duration.
/// </summary>
public sealed class Chapter : AuditableEntity
{
    private Chapter() { }

    public Guid SubjectId { get; private set; }
    public Guid GradeId   { get; private set; }

    public string  Title       { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>Display order within this Subject+Grade combination (1-based).</summary>
    public int Order { get; private set; }

    /// <summary>null = platform-wide; non-null = scoped to this tenant.</summary>
    public Guid? TenantId { get; private set; }

    // Reference navigation — EF Core populates these on explicit Include().
    public Subject Subject { get; private set; } = null!;
    public Grade   Grade   { get; private set; } = null!;

    public static Chapter Create(
        Guid    subjectId,
        Guid    gradeId,
        string  title,
        int     order,
        string? description = null,
        Guid?   tenantId    = null)
    {
        if (subjectId == Guid.Empty)
            throw new ArgumentException("SubjectId must not be empty.", nameof(subjectId));
        if (gradeId == Guid.Empty)
            throw new ArgumentException("GradeId must not be empty.", nameof(gradeId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (order < 1)
            throw new ArgumentOutOfRangeException(nameof(order), "Order must be ≥ 1.");

        return new Chapter
        {
            SubjectId   = subjectId,
            GradeId     = gradeId,
            Title       = title.Trim(),
            Description = description?.Trim(),
            Order       = order,
            TenantId    = tenantId,
            CreatedAt   = DateTime.UtcNow,
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates mutable fields. SubjectId and GradeId are fixed after creation.
    /// </summary>
    public void Update(string title, int order, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (order < 1)
            throw new ArgumentOutOfRangeException(nameof(order), "Order must be \u2265 1.");

        Title       = title.Trim();
        Description = description?.Trim();
        Order       = order;
        UpdatedAt   = DateTime.UtcNow;
    }
}
