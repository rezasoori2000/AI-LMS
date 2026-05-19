using LMS.Application.Teacher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers.Teacher;

/// <summary>
/// Read-only API for the teacher portal.
///
/// Access: Teacher role only.
/// All endpoints are scoped to students explicitly assigned to the calling teacher via
/// the <c>TeacherStudentAssignment</c> M:M join table.
/// Accessing an unassigned student returns 403 Forbidden.
/// </summary>
[ApiController]
[Route("api/teacher")]
[Authorize(Roles = "Teacher")]
public sealed class TeacherController : ControllerBase
{
    private readonly ITeacherService _teacher;

    public TeacherController(ITeacherService teacher) => _teacher = teacher;

    /// <summary>
    /// Returns dashboard summary stats for the calling teacher:
    /// assigned student count, active enrollment count, and completions in the past 7 days.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct) =>
        Ok(await _teacher.GetSummaryAsync(ct));

    /// <summary>
    /// Returns all students assigned to the calling teacher with per-student
    /// enrollment and completion summary counts.
    /// </summary>
    [HttpGet("students")]
    public async Task<IActionResult> GetMyStudents(CancellationToken ct) =>
        Ok(await _teacher.GetMyStudentsAsync(ct));

    /// <summary>
    /// Returns enrollment detail for one assigned student.
    /// </summary>
    /// <response code="200">Student detail with enrollment list.</response>
    /// <response code="403">The student is not assigned to the calling teacher.</response>
    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetStudentDetail(Guid studentId, CancellationToken ct) =>
        Ok(await _teacher.GetStudentDetailAsync(studentId, ct));

    /// <summary>
    /// Returns lesson-level progress for one assigned student across all active enrollments.
    /// </summary>
    /// <response code="200">Progress breakdown per lesson.</response>
    /// <response code="403">The student is not assigned to the calling teacher.</response>
    [HttpGet("students/{studentId:guid}/progress")]
    public async Task<IActionResult> GetStudentProgress(Guid studentId, CancellationToken ct) =>
        Ok(await _teacher.GetStudentProgressAsync(studentId, ct));
}
