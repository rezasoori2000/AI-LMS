using LMS.Application.AiTutor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers.Student;

/// <summary>
/// AI Tutor API for student-facing tutoring sessions.
///
/// MVP scope:
///   - Open a tutor session for the lesson the student is currently viewing.
///   - Ask free-text curriculum questions; receive AI-generated replies.
///   - Request a hint for a specific question (without revealing the correct answer).
///   - End the session explicitly.
///
/// All endpoints require a valid JWT with role "Student".
/// Enrollment and conversation-ownership checks are enforced inside
/// <see cref="ITutorService"/>; the controller is intentionally thin.
/// </summary>
[ApiController]
[Route("api/student/tutor")]
[Authorize(Roles = "Student")]
public sealed class TutorController : ControllerBase
{
    private readonly ITutorService _tutor;

    public TutorController(ITutorService tutor) => _tutor = tutor;

    // ── Start session ──────────────────────────────────────────────────────────

    /// <summary>
    /// Opens a new AI tutor conversation for a lesson.
    /// The student must be enrolled in the lesson's subject.
    /// Returns a <c>ConversationId</c> required by all subsequent /ask and /end calls.
    /// </summary>
    /// <response code="201">Session started. Body: <see cref="StartTutorSessionResponse"/>.</response>
    /// <response code="403">Student is not enrolled in the lesson's subject.</response>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(StartTutorSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StartSession(
        [FromBody] StartTutorSessionRequest request,
        CancellationToken ct)
    {
        var result = await _tutor.StartSessionAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // ── Ask ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a student message to the AI tutor and returns the tutor's reply.
    /// Persists both the student message and the AI reply to the conversation record.
    /// </summary>
    /// <param name="conversationId">The active conversation ID returned by StartSession.</param>
    /// <response code="200">AI tutor reply. Body: <see cref="TutorReplyDto"/>.</response>
    /// <response code="403">Conversation does not belong to the calling student.</response>
    /// <response code="409">Conversation has already been ended.</response>
    [HttpPost("sessions/{conversationId:guid}/ask")]
    [ProducesResponseType(typeof(TutorReplyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Ask(
        Guid conversationId,
        [FromBody] TutorAskRequest request,
        CancellationToken ct)
    {
        var result = await _tutor.AskAsync(conversationId, request, ct);
        return Ok(result);
    }

    // ── End session ────────────────────────────────────────────────────────────

    /// <summary>
    /// Ends the active tutor session (sets <c>AiConversation.EndedAt</c>).
    /// Idempotent — safe to call multiple times on the same conversation.
    /// </summary>
    /// <param name="conversationId">The conversation to end.</param>
    /// <response code="204">Session ended (or was already ended).</response>
    /// <response code="403">Conversation does not belong to the calling student.</response>
    [HttpPost("sessions/{conversationId:guid}/end")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> EndSession(
        Guid conversationId,
        CancellationToken ct)
    {
        await _tutor.EndSessionAsync(conversationId, ct);
        return NoContent();
    }
}

