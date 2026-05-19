using LMS.Domain.Catalog;
using LMS.Domain.Progress;

namespace LMS.Application.Student.Dtos;

// ============================================================
// Student portal read DTOs — Phase 1 MVP
// These records define the API contract for all student-facing
// endpoints.  No correct-answer data is exposed in any response.
// ============================================================

/// <summary>
/// Aggregate stats for the student dashboard stat row.
/// GET /api/student/summary
/// </summary>
public sealed record StudentSummaryDto(
    int     ActiveEnrollments,
    int     CompletedLessons,
    int     InProgressLessons,
    /// <summary>
    /// (completedLessons / totalLessonsAcrossActiveEnrollments) × 100.
    /// 0 when there are no active enrollments.
    /// </summary>
    decimal OverallProgressPercent);

/// <summary>
/// One enrolled subject shown in the enrollment list.
/// GET /api/student/enrollments
/// </summary>
public sealed record EnrolledSubjectDto(
    Guid      EnrollmentId,
    Guid      SubjectId,
    string    SubjectName,
    string    SubjectSlug,
    string?   GradeName,
    DateTime  EnrolledAt,
    int       TotalLessons,
    int       CompletedLessons,
    /// <summary>
    /// The first lesson that is NotStarted or InProgress within this subject,
    /// ordered by Chapter.Order then Lesson.Order.
    /// Null when all lessons are completed.
    /// </summary>
    Guid?     NextLessonId);

/// <summary>
/// Full chapter + lesson tree for one enrolled subject.
/// GET /api/student/subjects/{subjectId}/chapters
/// </summary>
public sealed record SubjectChaptersDto(
    Guid                        SubjectId,
    string                      SubjectName,
    List<ChapterWithLessonsDto> Chapters);

/// <summary>One chapter with its ordered lesson summaries.</summary>
public sealed record ChapterWithLessonsDto(
    Guid                   ChapterId,
    string                 ChapterTitle,
    int                    Order,
    List<LessonSummaryDto> Lessons);

/// <summary>Lightweight lesson row shown in the subject outline.</summary>
public sealed record LessonSummaryDto(
    Guid           LessonId,
    string         Title,
    int            Order,
    int?           EstimatedMinutes,
    ProgressStatus ProgressStatus);

/// <summary>
/// Full lesson content for the lesson viewer page.
/// GET /api/student/lessons/{lessonId}
///
/// Security note: CorrectAnswer is intentionally absent from QuestionForStudentDto.
/// The correct answer must never be included in any student-facing API response.
/// </summary>
public sealed record LessonDetailDto(
    Guid                        LessonId,
    string                      Title,
    string?                     Content,
    int?                        EstimatedMinutes,
    Guid                        ChapterId,
    string                      ChapterTitle,
    Guid                        SubjectId,
    string                      SubjectName,
    ProgressStatus              ProgressStatus,
    List<QuestionForStudentDto> Questions);

/// <summary>
/// A question as shown to a student — type, text, and options only.
/// CorrectAnswer is intentionally omitted.
/// </summary>
public sealed record QuestionForStudentDto(
    Guid         QuestionId,
    string       Text,
    QuestionType Type,
    /// <summary>
    /// Populated for MultipleChoice: array of option strings.
    /// Null for TrueFalse (client renders True/False buttons) and ShortAnswer.
    /// </summary>
    string[]?    Options);

// ============================================================
// Progress write DTOs
// ============================================================

/// <summary>
/// Request body for POST /api/student/lessons/{lessonId}/complete.
/// Answers may be empty for content-only lessons (no questions).
/// </summary>
public sealed record CompleteRequest(
    List<AnswerDto> Answers);

/// <summary>
/// One answer submission from the student.
/// Encoding by type:
///   MultipleChoice → zero-based option index as string ("0", "1", "2", "3")
///   TrueFalse      → "true" or "false" (lowercase)
///   ShortAnswer    → free text (not evaluated in Phase 1)
/// </summary>
public sealed record AnswerDto(
    Guid   QuestionId,
    string Answer);

/// <summary>
/// Response from POST /api/student/lessons/{lessonId}/complete.
/// </summary>
public sealed record CompleteResponse(
    /// <summary>
    /// 0.0–100.0 percentage, rounded to one decimal place.
    /// Null when the lesson has no MultipleChoice or TrueFalse questions.
    /// </summary>
    decimal?              ScorePercent,
    /// <summary>
    /// Number of MultipleChoice + TrueFalse answers that were correct.
    /// ShortAnswer questions are excluded from this count.
    /// </summary>
    int                   CorrectCount,
    /// <summary>Total number of gradable (MC + TF) questions in the lesson.</summary>
    int                   GradableQuestions,
    List<AnswerResultDto> Results);

/// <summary>
/// Per-question result returned after lesson completion.
/// Phase 1: IsCorrect indicates pass/fail; the correct answer itself is not revealed.
/// Phase 3: add CorrectAnswer and Explanation fields once the review flow is designed.
/// </summary>
public sealed record AnswerResultDto(
    Guid QuestionId,
    bool IsCorrect);
