using LMS.Application.Content.Lessons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "SuperAdmin,TenantAdmin,ContentEditor")]
public sealed class LessonsController : ControllerBase
{
    private readonly ILessonService _lessons;

    public LessonsController(ILessonService lessons) => _lessons = lessons;

    /// <summary>Returns lessons, optionally filtered by chapterId.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] Guid? chapterId, CancellationToken ct) =>
        Ok(await _lessons.GetListAsync(chapterId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await _lessons.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateLessonRequest request, CancellationToken ct)
    {
        var created = await _lessons.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateLessonRequest request, CancellationToken ct) =>
        Ok(await _lessons.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _lessons.DeleteAsync(id, ct);
        return NoContent();
    }
}
