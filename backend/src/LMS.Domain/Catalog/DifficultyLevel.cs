namespace LMS.Domain.Catalog;

/// <summary>
/// Difficulty classification for questions.
/// Used by the UI to filter the question bank and by the AI tutor to adjust pacing.
/// Phase 3: derive automatically from student performance data (adaptive difficulty).
/// </summary>
public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard,
}
