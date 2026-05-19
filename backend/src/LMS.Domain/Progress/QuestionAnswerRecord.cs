using LMS.Domain.Catalog;
using LMS.Domain.Common;
using LMS.Domain.Curriculum;
using LMS.Domain.Students;

namespace LMS.Domain.Progress;

/// <summary>
/// Durable record of a student's answer outcome for a single question at the moment
/// of lesson completion.
///
/// Design notes:
/// - One record per (StudentId, QuestionId) per lesson completion.
///   Phase 1 allows only one completion per lesson (LessonProgress unique constraint),
///   so the (StudentId, QuestionId) pair is naturally unique in Phase 1.
///   Phase 3 will introduce LessonAttempt; at that point, an AttemptId FK will be added
///   here to distinguish records from separate attempts.
///
/// - LessonId is stored here even though it is reachable via Question.LessonId.
///   Denormalising it avoids a join on the hottest learner-history query path:
///   "What questions did this student answer in lesson X?" and
///   "What was this student's question-level performance in this lesson?"
///
/// - IsCorrect is a binary outcome. The submitted answer text is intentionally NOT stored
///   to avoid retaining student content beyond what is educationally necessary.
///
/// - ShortAnswer questions produce IsCorrect = false in Phase 1 (AI grading not yet wired).
///   These records are still written to preserve a complete picture of all questions
///   encountered. Phase 3 should update IsCorrect via AI grading once ShortAnswer
///   evaluation is available.
///
/// - AnsweredAt captures the lesson completion timestamp, not a per-question timestamp.
///   Phase 3: per-question response timing can be added if the front-end tracks it.
///
/// What this enables for Phase 3 / AI tutor:
///   - "Which questions has this student answered incorrectly across multiple lessons?"
///   - "What difficulty level does this student consistently struggle with?"
///     (join QuestionAnswerRecord → Question.Difficulty)
///   - "What topics appear in their recent error pattern?"
///     (join → Question → Lesson → Chapter → Subject once Question.TopicId is added)
///   - Longitudinal performance trend (order by AnsweredAt, track ScorePercent over time)
///
/// Privacy notes:
///   - Binary IsCorrect only — no submitted text, no session metadata.
///   - Records are scoped to TenantId for multi-tenant isolation.
///   - Deletion follows the same student data-deletion policy as LessonProgress.
///
/// Deferred to Phase 3:
///   - AttemptId FK (for multi-attempt support)
///   - Per-question response time (requires front-end timing support)
///   - ShortAnswer AI grading and IsCorrect back-fill
///   - Question.TopicId for richer subject-area analysis
/// </summary>
public sealed class QuestionAnswerRecord : AuditableEntity
{
    private QuestionAnswerRecord() { }

    /// <summary>FK to the StudentProfile who answered this question.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>
    /// FK to the Lesson this question belongs to.
    /// Denormalised from Question.LessonId for query efficiency.
    /// </summary>
    public Guid LessonId { get; private set; }

    /// <summary>FK to the Question that was answered.</summary>
    public Guid QuestionId { get; private set; }

    /// <summary>
    /// True when the student's submitted answer matched the correct answer.
    /// Always false for ShortAnswer in Phase 1 (no AI grading yet).
    /// </summary>
    public bool IsCorrect { get; private set; }

    /// <summary>
    /// UTC timestamp of the lesson completion event that produced this record.
    /// Approximates the time the student answered this question.
    /// </summary>
    public DateTime AnsweredAt { get; private set; }

    /// <summary>null = platform-wide; non-null = scoped to this tenant.</summary>
    public Guid? TenantId { get; private set; }

    // Reference navigation.
    public StudentProfile Student  { get; private set; } = null!;
    public Lesson         Lesson   { get; private set; } = null!;
    public Question       Question { get; private set; } = null!;

    public static QuestionAnswerRecord Create(
        Guid  studentId,
        Guid  lessonId,
        Guid  questionId,
        bool  isCorrect,
        Guid? tenantId = null)
    {
        if (studentId  == Guid.Empty) throw new ArgumentException("StudentId must not be empty.",  nameof(studentId));
        if (lessonId   == Guid.Empty) throw new ArgumentException("LessonId must not be empty.",   nameof(lessonId));
        if (questionId == Guid.Empty) throw new ArgumentException("QuestionId must not be empty.", nameof(questionId));

        return new QuestionAnswerRecord
        {
            StudentId  = studentId,
            LessonId   = lessonId,
            QuestionId = questionId,
            IsCorrect  = isCorrect,
            AnsweredAt = DateTime.UtcNow,
            TenantId   = tenantId,
            CreatedAt  = DateTime.UtcNow,
        };
    }
}
