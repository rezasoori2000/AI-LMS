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
    string? ParentEmail,
    Guid?   TeacherUserId,
    string? TeacherFullName);

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

/// <summary>
/// A trimmed teacher user item used to populate the "assign teacher" dropdown.
/// TeacherUserId is the User.Id of the teacher (no TeacherProfile entity in Phase 1).
/// </summary>
public sealed record TeacherOptionDto(
    Guid   TeacherUserId,
    string FullName,
    string Email);

/// <summary>
/// Request body for PATCH /api/admin/students/{id}/teacher.
/// Set <see cref="TeacherUserId"/> to a valid teacher <see cref="Guid"/> to assign,
/// or to <c>null</c> to unassign the existing teacher.
/// </summary>
public sealed record AssignTeacherRequest(
    Guid? TeacherUserId);
