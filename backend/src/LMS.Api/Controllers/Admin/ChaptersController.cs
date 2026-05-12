using LMS.Application.Content.Chapters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "SuperAdmin,TenantAdmin,ContentEditor")]
public sealed class ChaptersController : ControllerBase
{
    private readonly IChapterService _chapters;

    public ChaptersController(IChapterService chapters) => _chapters = chapters;

    /// <summary>Returns chapters, optionally filtered by subjectId and/or gradeId.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? subjectId,
        [FromQuery] Guid? gradeId,
        CancellationToken ct) =>
        Ok(await _chapters.GetListAsync(subjectId, gradeId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await _chapters.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateChapterRequest request, CancellationToken ct)
    {
        var created = await _chapters.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateChapterRequest request, CancellationToken ct) =>
        Ok(await _chapters.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _chapters.DeleteAsync(id, ct);
        return NoContent();
    }
}
