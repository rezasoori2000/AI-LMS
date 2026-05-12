namespace LMS.Application.Content.Questions;

public interface IQuestionService
{
    Task<List<QuestionDto>> GetListAsync(Guid? lessonId = null, CancellationToken ct = default);
    Task<QuestionDto>       GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<QuestionDto>       CreateAsync(CreateQuestionRequest request, CancellationToken ct = default);
    Task<QuestionDto>       UpdateAsync(Guid id, UpdateQuestionRequest request, CancellationToken ct = default);
    Task                    DeleteAsync(Guid id, CancellationToken ct = default);
}
