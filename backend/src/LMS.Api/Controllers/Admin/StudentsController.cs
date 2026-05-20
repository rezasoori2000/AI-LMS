using LMS.Application.Admin.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers.Admin;

/// <summary>
/// Admin API for managing student–parent and student–teacher linkage.
///
/// Access: SuperAdmin and TenantAdmin only.
/// ContentEditor cannot manage student linkage — that is a purely administrative act.
/// </summary>
[ApiController]
[Route("api/admin/students")]
[Authorize(Roles = "SuperAdmin,TenantAdmin")]
public sealed class StudentsController : ControllerBase
{
    private readonly IStudentAdminService _students;

    public StudentsController(IStudentAdminService students) => _students = students;

    /// <summary>
    /// Returns all student profiles with their current parent and teacher linkage state.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetStudents(CancellationToken ct) =>
        Ok(await _students.GetStudentsAsync(ct));

    /// <summary>
    /// Returns all parent profiles as a lightweight option list for dropdowns.
    /// </summary>
    [HttpGet("parent-options")]
    public async Task<IActionResult> GetParentOptions(CancellationToken ct) =>
        Ok(await _students.GetParentOptionsAsync(ct));

    /// <summary>
    /// Returns all active teacher users as a lightweight option list for dropdowns.
    /// </summary>
    [HttpGet("teacher-options")]
    public async Task<IActionResult> GetTeacherOptions(CancellationToken ct) =>
        Ok(await _students.GetTeacherOptionsAsync(ct));

    /// <summary>
    /// Assigns or removes the parent link for a student.
    /// Set <c>parentProfileId</c> to a valid GUID to link, or <c>null</c> to unlink.
    /// </summary>
    /// <response code="200">The updated student linkage summary.</response>
    /// <response code="400">The specified parent profile ID does not exist.</response>
    /// <response code="404">The specified student profile ID does not exist.</response>
    [HttpPatch("{studentId:guid}/parent")]
    public async Task<IActionResult> AssignParent(
        Guid                studentId,
        AssignParentRequest request,
        CancellationToken   ct)
        => Ok(await _students.AssignParentAsync(studentId, request, ct));

    /// <summary>
    /// Assigns or removes the teacher for a student.
    /// Set <c>teacherUserId</c> to a valid teacher user GUID to assign, or <c>null</c> to unassign.
    /// </summary>
    /// <response code="200">The updated student linkage summary.</response>
    /// <response code="400">The specified teacher user ID does not exist or is not a teacher.</response>
    /// <response code="404">The specified student profile ID does not exist.</response>
    [HttpPatch("{studentId:guid}/teacher")]
    public async Task<IActionResult> AssignTeacher(
        Guid                 studentId,
        AssignTeacherRequest request,
        CancellationToken    ct)
        => Ok(await _students.AssignTeacherAsync(studentId, request, ct));
}
