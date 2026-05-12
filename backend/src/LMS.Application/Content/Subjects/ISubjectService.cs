namespace LMS.Application.Content.Subjects;

public interface ISubjectService
{
    Task<List<SubjectDto>> GetListAsync(CancellationToken ct = default);
    Task<SubjectDto>       GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SubjectDto>       CreateAsync(CreateSubjectRequest request, CancellationToken ct = default);
    Task<SubjectDto>       UpdateAsync(Guid id, UpdateSubjectRequest request, CancellationToken ct = default);
    Task                   DeleteAsync(Guid id, CancellationToken ct = default);
}
