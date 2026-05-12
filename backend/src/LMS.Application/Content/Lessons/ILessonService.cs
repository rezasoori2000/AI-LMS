namespace LMS.Application.Content.Lessons;

public interface ILessonService
{
    Task<List<LessonDto>> GetListAsync(Guid? chapterId = null, CancellationToken ct = default);
    Task<LessonDto>       GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<LessonDto>       CreateAsync(CreateLessonRequest request, CancellationToken ct = default);
    Task<LessonDto>       UpdateAsync(Guid id, UpdateLessonRequest request, CancellationToken ct = default);
    Task                  DeleteAsync(Guid id, CancellationToken ct = default);
}
