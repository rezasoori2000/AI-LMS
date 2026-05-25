using LMS.Domain.Common;

namespace LMS.Domain.Curriculum;

/// <summary>
/// A single lesson within a chapter.
///
/// Design notes:
/// - Content is stored as a plain Markdown string for Phase 1.
///   Phase 3 will introduce a ContentBlock table (structured JSON blocks) for rich media,
///   video, embedded exercises, etc. The Content column will be soft-deprecated then.
/// - Retrieval rule (Section 12): Lesson.Content is canonical source material.
///   Retrieval chunks/embeddings are derived artifacts and must be re-creatable from
///   canonical lesson content (never the source of truth).
/// - EstimatedMinutes is informational — displayed to students, not enforced.
/// - Phase 3: add IsPublished lifecycle, VideoUrl, attachments, prerequisite lesson IDs.
/// </summary>
public sealed class Lesson : AuditableEntity
{
    private Lesson() { }

    public Guid ChapterId { get; private set; }

    public string  Title   { get; private set; } = string.Empty;

    /// <summary>
    /// Markdown content for Phase 1.
    /// Phase 3: replaced by a normalised ContentBlock child table.
    /// </summary>
    public string? Content { get; private set; }

    /// <summary>Display order within the chapter (1-based).</summary>
    public int Order { get; private set; }

    /// <summary>Informational estimate shown to students; not enforced by the platform.</summary>
    public int? EstimatedMinutes { get; private set; }

    /// <summary>null = platform-wide; non-null = scoped to this tenant.</summary>
    public Guid? TenantId { get; private set; }

    // Reference navigation.
    public Chapter Chapter { get; private set; } = null!;

    public static Lesson Create(
        Guid    chapterId,
        string  title,
        int     order,
        string? content          = null,
        int?    estimatedMinutes = null,
        Guid?   tenantId         = null)
    {
        if (chapterId == Guid.Empty)
            throw new ArgumentException("ChapterId must not be empty.", nameof(chapterId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (order < 1)
            throw new ArgumentOutOfRangeException(nameof(order), "Order must be ≥ 1.");

        return new Lesson
        {
            ChapterId        = chapterId,
            Title            = title.Trim(),
            Content          = content,
            Order            = order,
            EstimatedMinutes = estimatedMinutes,
            TenantId         = tenantId,
            CreatedAt        = DateTime.UtcNow,
        };
    }

    /// <summary>Replaces the Markdown content of this lesson.</summary>
    public void UpdateContent(string? content)
    {
        Content   = content;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates mutable fields. ChapterId is fixed after creation.
    /// </summary>
    public void Update(string title, int order, string? content, int? estimatedMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (order < 1)
            throw new ArgumentOutOfRangeException(nameof(order), "Order must be \u2265 1.");

        Title            = title.Trim();
        Order            = order;
        Content          = content;
        EstimatedMinutes = estimatedMinutes;
        UpdatedAt        = DateTime.UtcNow;
    }
}
