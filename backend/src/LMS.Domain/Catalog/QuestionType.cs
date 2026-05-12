namespace LMS.Domain.Catalog;

/// <summary>
/// Supported question types for Phase 1.
/// Phase 3 will add: FillInTheBlank, Matching, Ordering, Essay.
/// </summary>
public enum QuestionType
{
    MultipleChoice,
    TrueFalse,
    ShortAnswer,
}
