using LMS.Application.Content.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "SuperAdmin,TenantAdmin,ContentEditor")]
public sealed class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjects;

    public SubjectsController(ISubjectService subjects) => _subjects = subjects;

    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken ct) =>
        Ok(await _subjects.GetListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await _subjects.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateSubjectRequest request, CancellationToken ct)
    {
        var created = await _subjects.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateSubjectRequest request, CancellationToken ct) =>
        Ok(await _subjects.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _subjects.DeleteAsync(id, ct);
        return NoContent();
    }
}
