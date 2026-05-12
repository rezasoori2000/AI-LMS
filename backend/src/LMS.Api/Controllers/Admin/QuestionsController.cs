using LMS.Application.Content.Questions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "SuperAdmin,TenantAdmin,ContentEditor")]
public sealed class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questions;

    public QuestionsController(IQuestionService questions) => _questions = questions;

    /// <summary>Returns questions, optionally filtered by lessonId.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] Guid? lessonId, CancellationToken ct) =>
        Ok(await _questions.GetListAsync(lessonId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await _questions.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateQuestionRequest request, CancellationToken ct)
    {
        var created = await _questions.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateQuestionRequest request, CancellationToken ct) =>
        Ok(await _questions.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _questions.DeleteAsync(id, ct);
        return NoContent();
    }
}
