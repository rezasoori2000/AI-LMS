namespace LMS.Application.Content.Chapters;

public interface IChapterService
{
    Task<List<ChapterDto>> GetListAsync(Guid? subjectId = null, Guid? gradeId = null, CancellationToken ct = default);
    Task<ChapterDto>       GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ChapterDto>       CreateAsync(CreateChapterRequest request, CancellationToken ct = default);
    Task<ChapterDto>       UpdateAsync(Guid id, UpdateChapterRequest request, CancellationToken ct = default);
    Task                   DeleteAsync(Guid id, CancellationToken ct = default);
}
