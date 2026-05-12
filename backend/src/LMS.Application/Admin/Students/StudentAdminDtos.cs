using System.ComponentModel.DataAnnotations;

namespace LMS.Application.Admin.Students;

/// <summary>
/// Represents one student row in the admin linkage management list.
/// </summary>
public sealed record StudentLinkSummaryDto(
    Guid    StudentId,
    string  FullName,
    string? GradeName,
    Guid?   ParentProfileId,
    string? ParentFullName,
    string? ParentEmail);

/// <summary>
/// A trimmed parent profile item used to populate the "assign parent" dropdown.
/// </summary>
public sealed record ParentOptionDto(
    Guid   ParentProfileId,
    string FullName,
    string Email);

/// <summary>
/// Request body for PATCH /api/admin/students/{id}/parent.
/// Set <see cref="ParentProfileId"/> to a valid <see cref="Guid"/> to link,
/// or to <c>null</c> to unlink the existing parent.
/// </summary>
public sealed record AssignParentRequest(
    Guid? ParentProfileId);
