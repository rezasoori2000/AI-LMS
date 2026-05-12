using System.ComponentModel.DataAnnotations;

namespace LMS.Application.Content.Lessons;

public sealed record LessonDto(
    Guid      Id,
    Guid      ChapterId,
    string    Title,
    string?   Content,
    int       Order,
    int?      EstimatedMinutes,
    Guid?     TenantId,
    DateTime  CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateLessonRequest(
                               Guid    ChapterId,
    [Required, MaxLength(500)] string  Title,
    [Range(1, int.MaxValue)]   int     Order,
                               string? Content          = null,
    [Range(1, 600)]            int?    EstimatedMinutes = null);

/// <summary>
/// ChapterId is fixed after creation; only metadata and content are editable.
/// </summary>
public sealed record UpdateLessonRequest(
    [Required, MaxLength(500)] string  Title,
    [Range(1, int.MaxValue)]   int     Order,
                               string? Content          = null,
    [Range(1, 600)]            int?    EstimatedMinutes = null);
