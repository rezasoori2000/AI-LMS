using LMS.Application.Parent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers.Parent;

/// <summary>
/// Read-only API for parent-facing portal features.
///
/// All endpoints require a valid JWT with role "Parent".
/// Ownership is enforced inside <see cref="IParentService"/> — the controller
/// performs no data-access logic itself.
/// </summary>
[ApiController]
[Route("api/parent")]
[Authorize(Roles = "Parent")]
public sealed class ParentController : ControllerBase
{
    private readonly IParentService _parent;

    public ParentController(IParentService parent) => _parent = parent;

    /// <summary>
    /// Returns all students linked to the calling parent.
    /// Returns an empty array when no students are linked — never 404.
    /// </summary>
    [HttpGet("children")]
    public async Task<IActionResult> GetMyChildren(CancellationToken ct) =>
        Ok(await _parent.GetMyChildrenAsync(ct));

    /// <summary>
    /// Returns full enrollment and progress detail for a single linked student.
    /// </summary>
    /// <response code="200">Student detail with enrollment summaries.</response>
    /// <response code="403">The student is not linked to the calling parent.</response>
    [HttpGet("children/{studentId:guid}")]
    public async Task<IActionResult> GetChildDetail(Guid studentId, CancellationToken ct) =>
        Ok(await _parent.GetChildDetailAsync(studentId, ct));
}
