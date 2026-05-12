using LMS.Application.Content.Grades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "SuperAdmin,TenantAdmin,ContentEditor")]
public sealed class GradesController : ControllerBase
{
    private readonly IGradeService _grades;

    public GradesController(IGradeService grades) => _grades = grades;

    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken ct) =>
        Ok(await _grades.GetListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await _grades.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateGradeRequest request, CancellationToken ct)
    {
        var created = await _grades.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateGradeRequest request, CancellationToken ct) =>
        Ok(await _grades.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _grades.DeleteAsync(id, ct);
        return NoContent();
    }
}
