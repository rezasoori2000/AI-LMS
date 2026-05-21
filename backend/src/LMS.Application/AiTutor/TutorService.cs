using LMS.Application.Common.Interfaces;
using LMS.Application.Student;
using LMS.Domain.Conversations;
using LMS.Domain.Enrollments;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.AiTutor;

/// <summary>
/// Orchestrates AI tutoring sessions for individual students.
///
/// Flow for AskAsync:
///   1. Resolve calling student's StudentProfile.Id via ICurrentUserService.UserId
///   2. Load AiConversation (tracked, including messages) and assert ownership
///   3. Assert conversation is not ended (→ TutorConversationEndedException → 409)
///   4. Assemble TutorContextSnapshot via TutorContextAssembler
///   5. Call ITutorProvider.GetReplyAsync (StubTutorProvider in Phase 1)
///   6. Persist student message + AI reply via AiConversation.AddMessage
///   7. Return TutorReplyDto
///
/// Data boundaries:
///   MUST NOT write LessonProgress, QuestionAnswerRecord, or Enrollment.
///   MUST NOT include CorrectAnswer in any context or AI payload.
///   MUST NOT make grading decisions.
///   See TutorBoundaryNotes.cs for the complete contract.
/// </summary>
public sealed class TutorService : ITutorService
{
    private readonly ILmsDbContext         _db;
    private readonly ICurrentUserService   _currentUser;
    private readonly ITutorProvider        _provider;
    private readonly TutorContextAssembler _assembler;

    public TutorService(
        ILmsDbContext       db,
        ICurrentUserService currentUser,
        ITutorProvider      provider)
    {
        _db          = db;
        _currentUser = currentUser;
        _provider    = provider;
        _assembler   = new TutorContextAssembler(db); // shares the same scoped DbContext
    }

    // ── StartSessionAsync ─────────────────────────────────────────────────────

    public async Task<StartTutorSessionResponse> StartSessionAsync(
        StartTutorSessionRequest request,
        CancellationToken        ct = default)
    {
        var studentProfileId = await ResolveStudentProfileIdAsync(ct)
            ?? throw new StudentAccessDeniedException("lesson", request.LessonId);

        // Verify the lesson exists and the student is enrolled in its subject.
        var lesson = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Chapter)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId, ct)
            ?? throw new StudentAccessDeniedException("lesson", request.LessonId);

        await AssertEnrolledAsync(studentProfileId, lesson.Chapter.SubjectId, ct);

        var conversation = AiConversation.Create(
            studentId: studentProfileId,
            lessonId:  request.LessonId,
            tenantId:  _currentUser.TenantId);

        _db.AiConversations.Add(conversation);
        await _db.SaveChangesAsync(ct);

        return new StartTutorSessionResponse(conversation.Id, request.LessonId);
    }

    // ── AskAsync ──────────────────────────────────────────────────────────────

    public async Task<TutorReplyDto> AskAsync(
        Guid              conversationId,
        TutorAskRequest   request,
        CancellationToken ct = default)
    {
        var studentProfileId = await ResolveStudentProfileIdAsync(ct)
            ?? throw new StudentAccessDeniedException("conversation", conversationId);

        // Load with messages for history assembly. No AsNoTracking — EF Core must track
        // the conversation so that new AiMessage entities added via AddMessage() are persisted.
        var conversation = await _db.AiConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && c.StudentId == studentProfileId, ct)
            ?? throw new StudentAccessDeniedException("conversation", conversationId);

        if (conversation.EndedAt.HasValue)
            throw new TutorConversationEndedException(conversationId);

        var context = await _assembler.AssembleAsync(
            studentProfileId, conversation, request.HintForQuestionId, ct);

        var providerResponse = await _provider.GetReplyAsync(
            new TutorProviderRequest(context, request.Message), ct);

        // AddMessage() returns the new entity, which we explicitly register with the
        // context. EF Core treats non-empty Guid PKs found in a backing-field navigation
        // as Unchanged (assuming already persisted). Calling Add() here forces Added state.
        var studentMsg = conversation.AddMessage(MessageRole.User,      request.Message);
        var tutorMsg   = conversation.AddMessage(MessageRole.Assistant, providerResponse.Reply, providerResponse.TokensUsed);
        _db.AiMessages.Add(studentMsg);
        _db.AiMessages.Add(tutorMsg);
        await _db.SaveChangesAsync(ct);

        return new TutorReplyDto(conversationId, providerResponse.Reply, providerResponse.TokensUsed);
    }

    // ── EndSessionAsync ───────────────────────────────────────────────────────

    public async Task EndSessionAsync(
        Guid              conversationId,
        CancellationToken ct = default)
    {
        var studentProfileId = await ResolveStudentProfileIdAsync(ct)
            ?? throw new StudentAccessDeniedException("conversation", conversationId);

        var conversation = await _db.AiConversations
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && c.StudentId == studentProfileId, ct)
            ?? throw new StudentAccessDeniedException("conversation", conversationId);

        conversation.End(); // idempotent
        await _db.SaveChangesAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<Guid?> ResolveStudentProfileIdAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return null;

        return await _db.StudentProfiles
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);
    }

    private async Task AssertEnrolledAsync(
        Guid              studentProfileId,
        Guid              subjectId,
        CancellationToken ct)
    {
        var isEnrolled = await _db.Enrollments
            .AsNoTracking()
            .AnyAsync(
                e => e.StudentId == studentProfileId
                  && e.SubjectId == subjectId
                  && e.Status == EnrollmentStatus.Active, ct);

        if (!isEnrolled)
            throw new StudentAccessDeniedException("subject", subjectId);
    }
}
