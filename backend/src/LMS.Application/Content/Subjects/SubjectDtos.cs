using System.ComponentModel.DataAnnotations;

namespace LMS.Application.Content.Subjects;

public sealed record SubjectDto(
    Guid      Id,
    string    Name,
    string    Slug,
    string?   Description,
    Guid?     TenantId,
    DateTime  CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateSubjectRequest(
    [Required, MaxLength(200)] string  Name,
    [Required, MaxLength(100)] string  Slug,
                               string? Description = null);

public sealed record UpdateSubjectRequest(
    [Required, MaxLength(200)] string  Name,
    [Required, MaxLength(100)] string  Slug,
                               string? Description = null);
