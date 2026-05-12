using System.ComponentModel.DataAnnotations;

namespace LMS.Application.Content.Grades;

public sealed record GradeDto(
    Guid      Id,
    string    Name,
    int       Level,
    Guid?     TenantId,
    DateTime  CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateGradeRequest(
    [Required, MaxLength(200)] string Name,
    [Range(1, int.MaxValue)]   int    Level);

public sealed record UpdateGradeRequest(
    [Required, MaxLength(200)] string Name,
    [Range(1, int.MaxValue)]   int    Level);
