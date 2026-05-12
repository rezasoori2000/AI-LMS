using LMS.Domain.Enrollments;

namespace LMS.Application.Parent.Dtos;

/// <summary>
/// Lightweight summary of one linked child shown on the children list page.
/// </summary>
public sealed record ChildSummaryDto(
    Guid      StudentId,
    string    FullName,
    string?   GradeName,
    int       ActiveEnrollments,
    int       LessonsCompleted,
    DateTime? LastActivityAt);

/// <summary>
/// Per-enrollment progress summary shown inside a child's detail view.
/// </summary>
public sealed record EnrollmentSummaryDto(
    Guid             EnrollmentId,
    Guid             SubjectId,
    string           SubjectName,
    EnrollmentStatus Status,
    DateTime         EnrolledAt,
    int              TotalLessons,
    int              CompletedLessons,
    int              InProgressLessons,
    decimal?         AverageScore);

/// <summary>
/// Full response for <c>GET /api/parent/children/{studentId}</c>.
/// </summary>
public sealed record ChildDetailResponse(
    ChildSummaryDto            Summary,
    List<EnrollmentSummaryDto> Enrollments);
