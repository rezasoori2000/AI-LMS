using System.ComponentModel.DataAnnotations;
using LMS.Domain.Catalog;

namespace LMS.Application.Content.Questions;

public sealed record QuestionDto(
    Guid            Id,
    Guid?           LessonId,
    string          Text,
    QuestionType    Type,
    DifficultyLevel Difficulty,
    string?         OptionsJson,
    string          CorrectAnswer,
    Guid?           TenantId,
    DateTime        CreatedAt,
    DateTime?       UpdatedAt);

public sealed record CreateQuestionRequest(
    [Required]               string          Text,
    [Required]               string          CorrectAnswer,
                             QuestionType    Type,
                             DifficultyLevel Difficulty,
    [MaxLength(4000)]        string?         OptionsJson = null,
                             Guid?           LessonId    = null);

public sealed record UpdateQuestionRequest(
    [Required]               string          Text,
    [Required]               string          CorrectAnswer,
                             QuestionType    Type,
                             DifficultyLevel Difficulty,
    [MaxLength(4000)]        string?         OptionsJson = null,
                             Guid?           LessonId    = null);
