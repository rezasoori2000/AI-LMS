using LMS.Domain.Common;
using LMS.Domain.Curriculum;

namespace LMS.Domain.Catalog;

/// <summary>
/// A question in the platform question bank.
///
/// Design notes:
/// - LessonId is nullable: questions can exist as standalone bank items not yet
///   attached to any lesson. The UI groups them by lesson when a link exists.
/// - Options are stored as a JSON array string for Phase 1 to avoid a QuestionOption
///   join table. Phase 3 will normalise this into a child table.
///   Example value: ["Option A","Option B","Option C","Option D"]
/// - CorrectAnswer encoding by type:
///     MultipleChoice → 0-based index string ("0", "1", "2", "3")
///     TrueFalse      → "true" or "false"
///     ShortAnswer    → sample/expected answer (AI validates, not exact-match)
/// - Phase 3: add Explanation, Tags, SourceReference, normalise OptionsJson to child table.
/// </summary>
public sealed class Question : AuditableEntity
{
    private Question() { }

    /// <summary>null = standalone bank question; set = attached to this lesson.</summary>
    public Guid? LessonId { get; private set; }

    public string          Text        { get; private set; } = string.Empty;
    public QuestionType    Type        { get; private set; }
    public DifficultyLevel Difficulty  { get; private set; }

    /// <summary>
    /// JSON array of option strings for MultipleChoice questions.
    /// null for TrueFalse (options are implied) and ShortAnswer.
    /// Phase 3: normalise to a QuestionOption child table.
    /// </summary>
    public string? OptionsJson { get; private set; }

    /// <summary>See type-specific encoding in the class summary.</summary>
    public string CorrectAnswer { get; private set; } = string.Empty;

    /// <summary>null = platform-wide; non-null = scoped to this tenant.</summary>
    public Guid? TenantId { get; private set; }

    // Reference navigation — null when LessonId is null.
    public Lesson? Lesson { get; private set; }

    public static Question Create(
        string          text,
        QuestionType    type,
        DifficultyLevel difficulty,
        string          correctAnswer,
        Guid?           lessonId    = null,
        string?         optionsJson = null,
        Guid?           tenantId    = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(correctAnswer);

        return new Question
        {
            LessonId      = lessonId,
            Text          = text.Trim(),
            Type          = type,
            Difficulty    = difficulty,
            OptionsJson   = optionsJson,
            CorrectAnswer = correctAnswer,
            TenantId      = tenantId,
            CreatedAt     = DateTime.UtcNow,
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void Update(
        string          text,
        QuestionType    type,
        DifficultyLevel difficulty,
        string          correctAnswer,
        string?         optionsJson,
        Guid?           lessonId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(correctAnswer);

        Text          = text.Trim();
        Type          = type;
        Difficulty    = difficulty;
        CorrectAnswer = correctAnswer.Trim();
        OptionsJson   = optionsJson;
        LessonId      = lessonId;
        UpdatedAt     = DateTime.UtcNow;
    }
}
