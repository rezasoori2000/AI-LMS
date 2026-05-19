namespace LMS.Application.Admin.Students;

/// <summary>
/// Admin service for managing student–parent and student–teacher linkage.
///
/// Roles that may call this service: SuperAdmin, TenantAdmin.
/// ContentEditor and below cannot manage linkage.
/// </summary>
public interface IStudentAdminService
{
    /// <summary>Returns all student profiles with their current parent and teacher linkage state.</summary>
    Task<List<StudentLinkSummaryDto>> GetStudentsAsync(CancellationToken ct = default);

    /// <summary>Returns all parent profiles as a lightweight option list for dropdowns.</summary>
    Task<List<ParentOptionDto>> GetParentOptionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns all active teacher users as a lightweight option list for dropdowns.
    /// </summary>
    Task<List<TeacherOptionDto>> GetTeacherOptionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Assigns or removes the parent link for a student.
    /// Pass <c>null</c> for <see cref="AssignParentRequest.ParentProfileId"/> to unlink.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown (→ 404) when <paramref name="studentId"/> does not exist.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown (→ 400) when <paramref name="request"/> specifies a
    /// <see cref="AssignParentRequest.ParentProfileId"/> that does not exist.
    /// </exception>
    Task<StudentLinkSummaryDto> AssignParentAsync(
        Guid                studentId,
        AssignParentRequest request,
        CancellationToken   ct = default);

    /// <summary>
    /// Assigns or removes the teacher for a student.
    /// Pass <c>null</c> for <see cref="AssignTeacherRequest.TeacherUserId"/> to unassign.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown (→ 404) when <paramref name="studentId"/> does not exist.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown (→ 400) when <paramref name="request"/> specifies a
    /// <see cref="AssignTeacherRequest.TeacherUserId"/> that does not exist or is not a Teacher.
    /// </exception>
    Task<StudentLinkSummaryDto> AssignTeacherAsync(
        Guid                 studentId,
        AssignTeacherRequest request,
        CancellationToken    ct = default);
}
