using LMS.Domain.Common;

namespace LMS.Domain.Curriculum;

/// <summary>
/// A subject or course offered on the platform (e.g., Mathematics, English, Science).
///
/// Design notes:
/// - Slug is URL-safe and must be unique per tenant (unique index enforced in Part 2).
/// - TenantId null  → platform-wide subject template.
/// - TenantId set   → tenant-customised subject.
/// - Phase 3: add curriculum-standard tags, IsPublished lifecycle, teacher assignment.
/// </summary>
public sealed class Subject : AuditableEntity
{
    private Subject() { }

    public string Name { get; private set; } = string.Empty;

    /// <summary>URL-safe identifier, e.g. "mathematics", unique within a tenant.</summary>
    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>null = platform-wide; non-null = scoped to this tenant.</summary>
    public Guid? TenantId { get; private set; }

    public static Subject Create(
        string  name,
        string  slug,
        string? description = null,
        Guid?   tenantId    = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return new Subject
        {
            Name        = name.Trim(),
            Slug        = slug.ToLowerInvariant().Trim(),
            Description = description?.Trim(),
            TenantId    = tenantId,
            CreatedAt   = DateTime.UtcNow,
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void Update(string name, string slug, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        Name        = name.Trim();
        Slug        = slug.ToLowerInvariant().Trim();
        Description = description?.Trim();
        UpdatedAt   = DateTime.UtcNow;
    }
}
