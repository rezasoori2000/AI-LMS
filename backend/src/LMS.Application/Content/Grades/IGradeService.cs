namespace LMS.Application.Content.Grades;

public interface IGradeService
{
    Task<List<GradeDto>> GetListAsync(CancellationToken ct = default);
    Task<GradeDto>       GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<GradeDto>       CreateAsync(CreateGradeRequest request, CancellationToken ct = default);
    Task<GradeDto>       UpdateAsync(Guid id, UpdateGradeRequest request, CancellationToken ct = default);
    Task                 DeleteAsync(Guid id, CancellationToken ct = default);
}
