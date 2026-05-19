using LMS.Application.Teacher.Dtos;

namespace LMS.Application.Teacher;

/// <summary>
/// Read-only service for teacher-facing portal queries.
///
/// Ownership model:
///   Every method resolves the calling teacher's User.Id from
///   <see cref="LMS.Application.Common.Interfaces.ICurrentUserService.UserId"/> and uses
///   the <c>TeacherStudentAssignment</c> M:M join table as the hard assignment gate.
///
///   Accessing a student not assigned to the calling teacher throws
///   <see cref="TeacherAccessDeniedException"/> → HTTP 403.
///
/// Phase 1 constraints:
///   - Read-only: no write operations are exposed.
///   - One student has at most one assigned teacher (admin-managed Phase 1 convention;
///     <c>TeacherStudentAssignment</c> stores the M:M relationship. Phase 3 will add a
///     <c>TeacherProfile</c> entity and subject-scoped co-teaching support).
///   - Students with no row in <c>TeacherStudentAssignment</c> are invisible to all teachers.
///   - <c>Question.CorrectAnswer</c> is never exposed in any teacher-facing DTO.
///   - Only active enrollments are shown in the progress detail view.
/// </summary>
public interface ITeacherService
{
    /// <summary>
    /// Returns aggregate stats for the calling teacher's dashboard stat row.
    /// Returns zero counts when no students are assigned — never throws.
    /// </summary>
    Task<TeacherSummaryDto> GetSummaryAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns all students assigned to the calling teacher with per-student
    /// enrollment and completion summary counts.
    /// Returns an empty list when no students are assigned — never throws.
    /// </summary>
    Task<List<AssignedStudentSummaryDto>> GetMyStudentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns enrollment-level detail for one assigned student.
    /// </summary>
    /// <exception cref="TeacherAccessDeniedException">
    /// Thrown (→ 403) when <paramref name="studentId"/> is not assigned to the calling teacher.
    /// </exception>
    Task<TeacherStudentDetailDto> GetStudentDetailAsync(
        Guid studentId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns lesson-level progress for one assigned student across all their
    /// active enrollments.
    /// </summary>
    /// <exception cref="TeacherAccessDeniedException">
    /// Thrown (→ 403) when <paramref name="studentId"/> is not assigned to the calling teacher.
    /// </exception>
    Task<TeacherStudentProgressDto> GetStudentProgressAsync(
        Guid studentId,
        CancellationToken ct = default);
}
