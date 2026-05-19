using LMS.Application.Student;
using LMS.Application.Student.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers.Student;

/// <summary>
/// API for student-facing portal features: dashboard summary, enrolled subjects,
/// lesson navigation, and lesson completion.
///
/// All endpoints require a valid JWT with role "Student".
/// Ownership and enrollment checks are enforced inside <see cref="IStudentService"/> —
/// the controller performs no data-access logic itself.
/// </summary>
[ApiController]
[Route("api/student")]
[Authorize(Roles = "Student")]
public sealed class StudentController : ControllerBase
{
    private readonly IStudentService _student;

    public StudentController(IStudentService student) => _student = student;

    // ── Dashboard ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns headline progress stats for the student dashboard.
    /// </summary>
    /// <response code="200">Summary counts and overall completion percentage.</response>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct) =>
        Ok(await _student.GetSummaryAsync(ct));

    // ── Enrollments ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all active enrollments with per-subject progress for the calling student.
    /// </summary>
    /// <response code="200">List of enrolled subjects with completion counts.</response>
    [HttpGet("enrollments")]
    public async Task<IActionResult> GetMyEnrollments(CancellationToken ct) =>
        Ok(await _student.GetMyEnrollmentsAsync(ct));

    // ── Subject chapters & lessons ────────────────────────────────────────────

    /// <summary>
    /// Returns the chapter + lesson tree for a subject the student is enrolled in.
    /// Each lesson includes the student's current progress status.
    /// </summary>
    /// <response code="200">Chapter tree with lessons and progress status.</response>
    /// <response code="403">Student is not enrolled in this subject.</response>
    [HttpGet("subjects/{subjectId:guid}/chapters")]
    public async Task<IActionResult> GetSubjectChapters(Guid subjectId, CancellationToken ct) =>
        Ok(await _student.GetSubjectChaptersAsync(subjectId, ct));

    // ── Lesson ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns lesson content and questions for the student to work through.
    /// CorrectAnswer is intentionally excluded from the response.
    /// </summary>
    /// <response code="200">Lesson detail with questions (no correct answers).</response>
    /// <response code="403">Student is not enrolled in the subject containing this lesson.</response>
    [HttpGet("lessons/{lessonId:guid}")]
    public async Task<IActionResult> GetLesson(Guid lessonId, CancellationToken ct) =>
        Ok(await _student.GetLessonAsync(lessonId, ct));

    /// <summary>
    /// Marks the lesson as InProgress (idempotent — safe to call multiple times).
    /// Should be called when the student opens the lesson player.
    /// </summary>
    /// <response code="200">Lesson is now InProgress (or was already in a later state).</response>
    /// <response code="403">Student is not enrolled in the subject containing this lesson.</response>
    [HttpPost("lessons/{lessonId:guid}/start")]
    public async Task<IActionResult> StartLesson(Guid lessonId, CancellationToken ct)
    {
        await _student.StartLessonAsync(lessonId, ct);
        return Ok();
    }

    /// <summary>
    /// Submits answers, scores the lesson, and marks it Completed.
    /// Returns per-question correctness and overall score (null for content-only lessons).
    /// </summary>
    /// <response code="200">Completion result with score and per-question feedback.</response>
    /// <response code="403">Student is not enrolled in the subject containing this lesson.</response>
    /// <response code="409">The lesson has already been completed.</response>
    [HttpPost("lessons/{lessonId:guid}/complete")]
    public async Task<IActionResult> CompleteLesson(
        Guid lessonId,
        [FromBody] CompleteRequest request,
        CancellationToken ct) =>
        Ok(await _student.CompleteLessonAsync(lessonId, request, ct));
}
