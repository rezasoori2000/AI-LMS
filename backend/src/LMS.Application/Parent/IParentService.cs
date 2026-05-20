using LMS.Application.Parent.Dtos;

namespace LMS.Application.Parent;

/// <summary>
/// Read-only service for parent-facing queries.
///
/// All operations are scoped to the calling parent's own linked students.
/// No write operations are exposed in Phase 1.
/// </summary>
public interface IParentService
{
    /// <summary>
    /// Returns summary information for all students linked to the calling parent.
    /// Returns an empty list when the parent has no linked students — never throws.
    /// </summary>
    Task<List<ChildSummaryDto>> GetMyChildrenAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the full detail for a single linked child, including enrollment and
    /// per-enrollment lesson progress aggregates.
    /// </summary>
    /// <exception cref="ParentAccessDeniedException">
    /// Thrown (→ 403) when <paramref name="studentId"/> is not linked to the calling parent.
    /// </exception>
    Task<ChildDetailDto> GetChildDetailAsync(Guid studentId, CancellationToken ct = default);
}
