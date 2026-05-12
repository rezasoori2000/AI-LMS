using System.ComponentModel.DataAnnotations;

namespace LMS.Application.Content.Chapters;

public sealed record ChapterDto(
    Guid      Id,
    Guid      SubjectId,
    Guid      GradeId,
    string    Title,
    string?   Description,
    int       Order,
    Guid?     TenantId,
    DateTime  CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateChapterRequest(
                               Guid    SubjectId,
                               Guid    GradeId,
    [Required, MaxLength(500)] string  Title,
    [Range(1, int.MaxValue)]   int     Order,
                               string? Description = null);

/// <summary>
/// SubjectId and GradeId are fixed after creation; only metadata is editable.
/// </summary>
public sealed record UpdateChapterRequest(
    [Required, MaxLength(500)] string  Title,
    [Range(1, int.MaxValue)]   int     Order,
                               string? Description = null);
