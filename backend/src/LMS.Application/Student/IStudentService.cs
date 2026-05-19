using LMS.Application.Student.Dtos;

namespace LMS.Application.Student;

/// <summary>
/// Read and write service for student-facing portal features.
///
/// Ownership model:
///   Every method resolves the calling student's <c>StudentProfile.Id</c> from
///   <see cref="LMS.Application.Common.Interfaces.ICurrentUserService.UserId"/> and uses
///   it as the hard ownership gate.
///
///   Lesson content is only accessible when the student has an Active enrollment in
///   the lesson's parent subject.  Any access to a non-enrolled subject or its lessons
///   throws <see cref="StudentAccessDeniedException"/> → HTTP 403.
///
/// Write idempotency:
///   <see cref="StartLessonAsync"/> is idempotent — calling it on an already-started or
///   completed lesson is a no-op (returns successfully).
///   <see cref="CompleteLessonAsync"/> throws if the lesson is already completed.
///
/// Phase 1 constraints:
///   - ShortAnswer questions are excluded from scoring (returned to the student but not
///     evaluated; score is calculated only from MultipleChoice and TrueFalse answers).
///   - Lesson access is unrestricted within an enrolled subject (no sequential lock).
///     Phase 3 can introduce prerequisite lesson gating if required.
///   - One LessonProgress record per (StudentId, LessonId) pair.  Multiple attempts
///     will be introduced in Phase 3 via a LessonAttempt child table.
/// </summary>
public interface IStudentService
{
    // ── Dashboard ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns aggregate stats for the calling student's dashboard stat row.
    /// </summary>
    Task<StudentSummaryDto> GetSummaryAsync(CancellationToken ct = default);

    // ── Enrollments / Subject browsing ────────────────────────────────────────

    /// <summary>
    /// Returns all active enrollments for the calling student with per-subject
    /// lesson-completion totals and a pointer to the next unstarted lesson.
    /// Returns an empty list when the student has no active enrollments.
    /// </summary>
    Task<List<EnrolledSubjectDto>> GetMyEnrollmentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the chapters and lessons for an enrolled subject, including each
    /// lesson's current progress status for the calling student.
    /// </summary>
    /// <exception cref="StudentAccessDeniedException">
    /// Thrown (→ 403) when the student is not enrolled in <paramref name="subjectId"/>.
    /// </exception>
    Task<SubjectChaptersDto> GetSubjectChaptersAsync(Guid subjectId, CancellationToken ct = default);

    // ── Lesson delivery ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the full lesson content and attached questions for viewing.
    /// Questions include type, text, and options — but <b>never</b> the correct answer.
    /// </summary>
    /// <exception cref="StudentAccessDeniedException">
    /// Thrown (→ 403) when the student is not enrolled in the lesson's parent subject.
    /// </exception>
    Task<LessonDetailDto> GetLessonAsync(Guid lessonId, CancellationToken ct = default);

    // ── Progress writes ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates or updates the <c>LessonProgress</c> record for this lesson to InProgress.
    /// Idempotent — no-op when the lesson is already InProgress or Completed.
    /// </summary>
    /// <exception cref="StudentAccessDeniedException">
    /// Thrown (→ 403) when the student is not enrolled in the lesson's parent subject.
    /// </exception>
    Task StartLessonAsync(Guid lessonId, CancellationToken ct = default);

    /// <summary>
    /// Marks the lesson as Completed, validates any submitted answers, and calculates
    /// the percentage score for lessons with auto-graded questions.
    ///
    /// Scoring rules:
    ///   - MultipleChoice and TrueFalse answers are evaluated server-side.
    ///   - ShortAnswer questions are included in <c>Results</c> but excluded from scoring
    ///     (IsCorrect is always false in Phase 1; AI evaluation deferred to Phase 3).
    ///   - ScorePercent is null when the lesson has no gradable questions.
    ///   - Score = (correct MC+TF answers) / (total MC+TF questions) × 100.
    /// </summary>
    /// <exception cref="StudentAccessDeniedException">
    /// Thrown (→ 403) when the student is not enrolled in the lesson's parent subject.
    /// </exception>
    /// <exception cref="LessonAlreadyCompletedException">
    /// Thrown (→ 409) when the lesson is already completed (one attempt per lesson in Phase 1).
    /// </exception>
    Task<CompleteResponse> CompleteLessonAsync(
        Guid            lessonId,
        CompleteRequest request,
        CancellationToken ct = default);
}
