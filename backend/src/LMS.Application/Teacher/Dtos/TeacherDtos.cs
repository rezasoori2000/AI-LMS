using LMS.Domain.Enrollments;
using LMS.Domain.Progress;

namespace LMS.Application.Teacher.Dtos;

/// <summary>Summary stats for the teacher dashboard stat row.</summary>
public sealed record TeacherSummaryDto(
    int TotalAssignedStudents,
    int ActiveEnrollments,
    int LessonsCompletedThisWeek);

/// <summary>One student row in the teacher's assigned-students list.</summary>
public sealed record AssignedStudentSummaryDto(
    Guid    StudentId,
    string  FullName,
    string  Email,
    string? GradeName,
    int     ActiveEnrollmentCount,
    int     TotalLessonsCompleted);

/// <summary>
/// Full student detail with enrollment list, used in the teacher monitoring view.
/// </summary>
public sealed record TeacherStudentDetailDto(
    Guid   StudentId,
    string FullName,
    string Email,
    string? GradeName,
    List<TeacherEnrollmentItemDto> Enrollments);

/// <summary>Per-enrollment summary within a student detail view.</summary>
public sealed record TeacherEnrollmentItemDto(
    Guid             EnrollmentId,
    Guid             SubjectId,
    string           SubjectName,
    EnrollmentStatus Status,
    DateTime         EnrolledAt,
    int              LessonsTotal,
    int              LessonsCompleted,
    int              LessonsInProgress,
    DateTime?        LastActivityAt);

/// <summary>
/// Lesson-level progress breakdown for one student, scoped to their active enrollments.
/// </summary>
public sealed record TeacherStudentProgressDto(
    Guid   StudentId,
    string FullName,
    List<TeacherLessonProgressItemDto> LessonProgress);

/// <summary>One lesson row in the per-student progress detail view.</summary>
public sealed record TeacherLessonProgressItemDto(
    Guid           LessonId,
    string         LessonTitle,
    Guid           SubjectId,
    string         SubjectName,
    string         ChapterTitle,
    ProgressStatus Status,
    decimal?       ScorePercent,
    DateTime?      StartedAt,
    DateTime?      CompletedAt);
