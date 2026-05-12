using LMS.Domain.Common;
using LMS.Domain.Curriculum;
using LMS.Domain.Students;

namespace LMS.Domain.Progress;

/// <summary>
/// Tracks a student's progress through a single lesson.
///
/// Design notes:
/// - The (StudentId, LessonId) pair must be unique — unique index enforced in Part 2.
/// - ScorePercent (0–100) is only meaningful for lessons that end with auto-graded
///   questions. It is null for content-only lessons.
/// - Completed is a terminal state in Phase 1 — there is no "re-attempt" concept yet.
///   Phase 3 will introduce a LessonAttempt child table to record multiple scored attempts,
///   keeping LessonProgress as a summary of best/latest result.
/// - Phase 3: add AttemptsCount, TimeSpentSeconds, PassedAt, TeacherFeedback.
/// </summary>
public sealed class LessonProgress : AuditableEntity
{
    private LessonProgress() { }

    /// <summary>FK to the StudentProfile whose progress this record tracks.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>FK to the Lesson being tracked.</summary>
    public Guid LessonId { get; private set; }

    public ProgressStatus Status { get; private set; }

    public DateTime? StartedAt   { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// 0.0–100.0 percentage score from auto-graded questions.
    /// null when the lesson has no questions, or when not yet completed.
    /// </summary>
    public decimal? ScorePercent { get; private set; }

    /// <summary>null = platform-wide; non-null = scoped to this tenant.</summary>
    public Guid? TenantId { get; private set; }

    // Reference navigation.
    public StudentProfile Student { get; private set; } = null!;
    public Lesson         Lesson  { get; private set; } = null!;

    public static LessonProgress Create(Guid studentId, Guid lessonId, Guid? tenantId = null)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("StudentId must not be empty.", nameof(studentId));
        if (lessonId == Guid.Empty)
            throw new ArgumentException("LessonId must not be empty.", nameof(lessonId));

        return new LessonProgress
        {
            StudentId = studentId,
            LessonId  = lessonId,
            Status    = ProgressStatus.NotStarted,
            TenantId  = tenantId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Marks the lesson as started.
    /// No-op when already InProgress or Completed (idempotent for duplicate events).
    /// </summary>
    public void Start()
    {
        if (Status != ProgressStatus.NotStarted) return;

        Status    = ProgressStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the lesson as completed, optionally recording a percentage score.
    /// If Start() was never called, StartedAt is set to now (defensive path).
    /// </summary>
    /// <param name="scorePercent">0.0–100.0 inclusive, or null for content-only lessons.</param>
    /// <exception cref="InvalidOperationException">Thrown if already completed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if score is outside 0–100.</exception>
    public void Complete(decimal? scorePercent = null)
    {
        if (Status == ProgressStatus.Completed)
            throw new InvalidOperationException("Lesson progress is already completed.");

        if (scorePercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(scorePercent), "Score must be between 0 and 100.");

        if (Status == ProgressStatus.NotStarted)
            StartedAt = DateTime.UtcNow;

        Status       = ProgressStatus.Completed;
        CompletedAt  = DateTime.UtcNow;
        ScorePercent = scorePercent;
        UpdatedAt    = DateTime.UtcNow;
    }
}
