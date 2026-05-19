using System.Text.Json;
using LMS.Application.Common.Interfaces;
using LMS.Application.Student.Dtos;
using LMS.Domain.Catalog;
using LMS.Domain.Enrollments;
using LMS.Domain.Progress;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student;

/// <summary>
/// EF Core implementation of <see cref="IStudentService"/>.
///
/// Access control:
///   Every public method calls <see cref="ResolveStudentProfileIdAsync"/> to resolve the
///   calling user's <c>StudentProfile.Id</c> and uses it as the ownership gate.
///   Methods that require enrollment access call <see cref="AssertEnrolledAsync"/> which
///   throws <see cref="StudentAccessDeniedException"/> (→ HTTP 403) when the student has
///   no active enrollment in the target subject.
///
/// Query strategy (N+1 avoidance):
///   Aggregate counts are computed in dedicated GroupBy queries returning dictionaries,
///   never via per-entity sub-loops.  This is the same pattern used by ParentService.
///
/// Phase 1 constraints (see IStudentService for full list):
///   - Free lesson access within enrolled subjects (no sequential gating).
///   - ShortAnswer is returned in the question list but excluded from scoring.
///   - ScorePercent and CorrectAnswer are never returned in any question DTO.
/// </summary>
public sealed class StudentService : IStudentService
{
    private readonly ILmsDbContext       _db;
    private readonly ICurrentUserService _currentUser;

    public StudentService(ILmsDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    public async Task<StudentSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var studentProfileId = await ResolveStudentProfileIdAsync(ct);
        if (studentProfileId is null)
            return new StudentSummaryDto(0, 0, 0, 0m);

        var activeEnrollments = await _db.Enrollments
            .AsNoTracking()
            .CountAsync(e => e.StudentId == studentProfileId
                          && e.Status == EnrollmentStatus.Active, ct);

        if (activeEnrollments == 0)
            return new StudentSummaryDto(0, 0, 0, 0m);

        // Progress counts across all enrolled subjects
        var progressCounts = await _db.LessonProgress
            .AsNoTracking()
            .Where(p => p.StudentId == studentProfileId)
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var completed   = progressCounts.FirstOrDefault(x => x.Status == ProgressStatus.Completed)?.Count   ?? 0;
        var inProgress  = progressCounts.FirstOrDefault(x => x.Status == ProgressStatus.InProgress)?.Count  ?? 0;

        // Total lessons across all actively enrolled subjects
        var activeSubjectIds = await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentProfileId
                     && e.Status == EnrollmentStatus.Active)
            .Select(e => e.SubjectId)
            .ToListAsync(ct);

        var totalLessons = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Chapter)
            .Where(l => activeSubjectIds.Contains(l.Chapter.SubjectId))
            .CountAsync(ct);

        var overallPercent = totalLessons > 0
            ? Math.Round((decimal)completed / totalLessons * 100, 1)
            : 0m;

        return new StudentSummaryDto(activeEnrollments, completed, inProgress, overallPercent);
    }

    // ── Enrollments ───────────────────────────────────────────────────────────

    public async Task<List<EnrolledSubjectDto>> GetMyEnrollmentsAsync(CancellationToken ct = default)
    {
        var studentProfileId = await ResolveStudentProfileIdAsync(ct);
        if (studentProfileId is null)
            return [];

        var enrollments = await _db.Enrollments
            .AsNoTracking()
            .Include(e => e.Subject)
            .Where(e => e.StudentId == studentProfileId
                     && e.Status == EnrollmentStatus.Active)
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync(ct);

        if (enrollments.Count == 0)
            return [];

        var subjectIds = enrollments.Select(e => e.SubjectId).ToList();

        // Total lessons per subject
        var totalLessons = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Chapter)
            .Where(l => subjectIds.Contains(l.Chapter.SubjectId))
            .GroupBy(l => l.Chapter.SubjectId)
            .Select(g => new { SubjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SubjectId, x => x.Count, ct);

        // Completed lessons per subject for this student
        var completedBySubject = await _db.LessonProgress
            .AsNoTracking()
            .Include(p => p.Lesson)
                .ThenInclude(l => l.Chapter)
            .Where(p => p.StudentId == studentProfileId
                     && p.Status == ProgressStatus.Completed)
            .Select(p => p.Lesson.Chapter.SubjectId)
            .ToListAsync(ct);

        var completedCounts = completedBySubject
            .GroupBy(sid => sid)
            .ToDictionary(g => g.Key, g => g.Count());

        // All lesson progress rows for this student (used to find NextLessonId)
        // Map: LessonId → ProgressStatus
        var progressByLesson = await _db.LessonProgress
            .AsNoTracking()
            .Where(p => p.StudentId == studentProfileId)
            .Select(p => new { p.LessonId, p.Status })
            .ToDictionaryAsync(x => x.LessonId, x => x.Status, ct);

        // For each subject, find the first lesson that is not Completed, ordered by
        // Chapter.Order then Lesson.Order (display order only — no gating in Phase 1).
        var lessonsOrdered = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Chapter)
            .Where(l => subjectIds.Contains(l.Chapter.SubjectId))
            .OrderBy(l => l.Chapter.SubjectId)
            .ThenBy(l => l.Chapter.Order)
            .ThenBy(l => l.Order)
            .Select(l => new { l.Id, l.Chapter.SubjectId })
            .ToListAsync(ct);

        var nextLessonBySubject = lessonsOrdered
            .GroupBy(l => l.SubjectId)
            .ToDictionary(
                g => g.Key,
                g => g.FirstOrDefault(l =>
                    !progressByLesson.TryGetValue(l.Id, out var s) || s != ProgressStatus.Completed)?.Id
            );

        // Resolve grade name via StudentProfile.GradeId — one optional lookup
        var studentProfile = await _db.StudentProfiles
            .AsNoTracking()
            .Include(s => s.Grade)
            .FirstOrDefaultAsync(s => s.Id == studentProfileId, ct);

        var gradeName = studentProfile?.Grade?.Name;

        return enrollments.Select(e => new EnrolledSubjectDto(
            EnrollmentId:     e.Id,
            SubjectId:        e.SubjectId,
            SubjectName:      e.Subject.Name,
            SubjectSlug:      e.Subject.Slug,
            GradeName:        gradeName,
            EnrolledAt:       e.EnrolledAt,
            TotalLessons:     totalLessons.GetValueOrDefault(e.SubjectId, 0),
            CompletedLessons: completedCounts.GetValueOrDefault(e.SubjectId, 0),
            NextLessonId:     nextLessonBySubject.GetValueOrDefault(e.SubjectId)
        )).ToList();
    }

    // ── Subject chapter tree ──────────────────────────────────────────────────

    public async Task<SubjectChaptersDto> GetSubjectChaptersAsync(
        Guid subjectId,
        CancellationToken ct = default)
    {
        var studentProfileId = await ResolveStudentProfileIdAsync(ct);
        await AssertEnrolledAsync(studentProfileId, subjectId, ct);

        var subject = await _db.Subjects
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == subjectId, ct);

        // subject will exist because AssertEnrolledAsync confirmed the enrollment
        var subjectName = subject?.Name ?? string.Empty;

        var chapters = await _db.Chapters
            .AsNoTracking()
            .Where(c => c.SubjectId == subjectId)
            .OrderBy(c => c.Order)
            .ToListAsync(ct);

        if (chapters.Count == 0)
            return new SubjectChaptersDto(subjectId, subjectName, []);

        var chapterIds = chapters.Select(c => c.Id).ToList();

        var lessons = await _db.Lessons
            .AsNoTracking()
            .Where(l => chapterIds.Contains(l.ChapterId))
            .OrderBy(l => l.ChapterId)
            .ThenBy(l => l.Order)
            .ToListAsync(ct);

        // Progress for this student on all lessons in these chapters
        var lessonIds = lessons.Select(l => l.Id).ToList();

        var progressMap = await _db.LessonProgress
            .AsNoTracking()
            .Where(p => p.StudentId == studentProfileId && lessonIds.Contains(p.LessonId))
            .Select(p => new { p.LessonId, p.Status })
            .ToDictionaryAsync(x => x.LessonId, x => x.Status, ct);

        var lessonsByChapter = lessons
            .GroupBy(l => l.ChapterId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var chapterDtos = chapters.Select(c =>
        {
            var chapterLessons = lessonsByChapter.GetValueOrDefault(c.Id, []);
            var lessonDtos = chapterLessons.Select(l => new LessonSummaryDto(
                LessonId:         l.Id,
                Title:            l.Title,
                Order:            l.Order,
                EstimatedMinutes: l.EstimatedMinutes,
                ProgressStatus:   progressMap.GetValueOrDefault(l.Id, ProgressStatus.NotStarted)
            )).ToList();

            return new ChapterWithLessonsDto(c.Id, c.Title, c.Order, lessonDtos);
        }).ToList();

        return new SubjectChaptersDto(subjectId, subjectName, chapterDtos);
    }

    // ── Lesson detail ─────────────────────────────────────────────────────────

    public async Task<LessonDetailDto> GetLessonAsync(
        Guid lessonId,
        CancellationToken ct = default)
    {
        var studentProfileId = await ResolveStudentProfileIdAsync(ct);

        // Resolve lesson + chapter + subject in one query
        var lesson = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Chapter)
                .ThenInclude(c => c.Subject)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson is null)
            throw new StudentAccessDeniedException("lesson", lessonId);

        await AssertEnrolledAsync(studentProfileId, lesson.Chapter.SubjectId, ct);

        var progress = await _db.LessonProgress
            .AsNoTracking()
            .Where(p => p.StudentId == studentProfileId && p.LessonId == lessonId)
            .Select(p => p.Status)
            .FirstOrDefaultAsync(ct);

        // Questions — CorrectAnswer is intentionally excluded from the projection
        var questions = await _db.Questions
            .AsNoTracking()
            .Where(q => q.LessonId == lessonId)
            .OrderBy(q => q.CreatedAt)
            .Select(q => new
            {
                q.Id,
                q.Text,
                q.Type,
                q.OptionsJson,
            })
            .ToListAsync(ct);

        var questionDtos = questions.Select(q =>
        {
            string[]? options = null;
            if (q.Type == QuestionType.MultipleChoice && q.OptionsJson is not null)
            {
                options = JsonSerializer.Deserialize<string[]>(q.OptionsJson);
            }
            return new QuestionForStudentDto(q.Id, q.Text, q.Type, options);
        }).ToList();

        return new LessonDetailDto(
            LessonId:         lesson.Id,
            Title:            lesson.Title,
            Content:          lesson.Content,
            EstimatedMinutes: lesson.EstimatedMinutes,
            ChapterId:        lesson.ChapterId,
            ChapterTitle:     lesson.Chapter.Title,
            SubjectId:        lesson.Chapter.SubjectId,
            SubjectName:      lesson.Chapter.Subject.Name,
            ProgressStatus:   progress,
            Questions:        questionDtos);
    }

    // ── Progress writes ───────────────────────────────────────────────────────

    public async Task StartLessonAsync(Guid lessonId, CancellationToken ct = default)
    {
        var studentProfileId = await ResolveStudentProfileIdAsync(ct);

        var lesson = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Chapter)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson is null)
            throw new StudentAccessDeniedException("lesson", lessonId);

        await AssertEnrolledAsync(studentProfileId, lesson.Chapter.SubjectId, ct);

        // Idempotent — if a progress record already exists, call Start() (no-op if already
        // InProgress or Completed), then save only if anything changed.
        var progressRecord = await _db.LessonProgress
            .FirstOrDefaultAsync(p => p.StudentId == studentProfileId && p.LessonId == lessonId, ct);

        if (progressRecord is null)
        {
            progressRecord = LessonProgress.Create(studentProfileId!.Value, lessonId);
            progressRecord.Start();
            _db.LessonProgress.Add(progressRecord);
        }
        else if (progressRecord.Status == ProgressStatus.NotStarted)
        {
            progressRecord.Start();
        }
        // Already InProgress or Completed — no-op

        await _db.SaveChangesAsync(ct);
    }

    public async Task<CompleteResponse> CompleteLessonAsync(
        Guid            lessonId,
        CompleteRequest request,
        CancellationToken ct = default)
    {
        var studentProfileId = await ResolveStudentProfileIdAsync(ct);

        var lesson = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Chapter)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson is null)
            throw new StudentAccessDeniedException("lesson", lessonId);

        await AssertEnrolledAsync(studentProfileId, lesson.Chapter.SubjectId, ct);

        // Load or create the progress record (without AsNoTracking — we will mutate it)
        var progressRecord = await _db.LessonProgress
            .FirstOrDefaultAsync(p => p.StudentId == studentProfileId && p.LessonId == lessonId, ct);

        if (progressRecord is null)
        {
            progressRecord = LessonProgress.Create(studentProfileId!.Value, lessonId);
            _db.LessonProgress.Add(progressRecord);
        }

        // Domain method throws InvalidOperationException when already Completed
        // (one attempt per lesson in Phase 1).

        // ── Scoring ───────────────────────────────────────────────────────────
        // Load only gradable questions (MC + TF) for scoring.
        // ShortAnswer questions are loaded for result completeness but not scored.
        var allQuestions = await _db.Questions
            .AsNoTracking()
            .Where(q => q.LessonId == lessonId)
            .Select(q => new { q.Id, q.Type, q.CorrectAnswer })
            .ToListAsync(ct);

        var answerMap = request.Answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.First().Answer);

        var results = new List<AnswerResultDto>();
        int correctCount  = 0;
        int gradableCount = 0;

        foreach (var question in allQuestions)
        {
            if (question.Type == QuestionType.ShortAnswer)
            {
                // Phase 1: ShortAnswer not evaluated — always false
                results.Add(new AnswerResultDto(question.Id, false));
                continue;
            }

            // MultipleChoice or TrueFalse
            gradableCount++;

            var submitted = answerMap.GetValueOrDefault(question.Id);
            bool isCorrect = submitted is not null
                && string.Equals(submitted.Trim(), question.CorrectAnswer.Trim(),
                                  StringComparison.OrdinalIgnoreCase);

            if (isCorrect) correctCount++;
            results.Add(new AnswerResultDto(question.Id, isCorrect));
        }

        decimal? scorePercent = gradableCount > 0
            ? Math.Round((decimal)correctCount / gradableCount * 100, 1)
            : null;

        try
        {
            progressRecord.Complete(scorePercent);
        }
        catch (InvalidOperationException)
        {
            throw new LessonAlreadyCompletedException(lessonId);
        }

        // Persist per-question answer outcomes alongside the progress record.
        // This data is the minimum durable learner-history signal for Phase 3 personalization:
        // it allows "where does this student struggle?" queries without requiring a full
        // event store.  The records are computed for free during scoring above; not storing
        // them would permanently discard the signal.
        foreach (var q in allQuestions)
        {
            var answered = results.First(r => r.QuestionId == q.Id);
            _db.QuestionAnswerRecords.Add(
                QuestionAnswerRecord.Create(
                    studentId:  studentProfileId!.Value,
                    lessonId:   lessonId,
                    questionId: q.Id,
                    isCorrect:  answered.IsCorrect,
                    tenantId:   progressRecord.TenantId));
        }

        await _db.SaveChangesAsync(ct);

        return new CompleteResponse(scorePercent, correctCount, gradableCount, results);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the StudentProfile.Id for the calling user.
    /// Returns null when no StudentProfile exists (admin-managed gap — see architecture doc).
    /// </summary>
    private async Task<Guid?> ResolveStudentProfileIdAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return null;

        return await _db.StudentProfiles
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Asserts that the student has an active enrollment in <paramref name="subjectId"/>.
    /// Throws <see cref="StudentAccessDeniedException"/> (→ HTTP 403) when not enrolled.
    /// A null <paramref name="studentProfileId"/> is treated as not enrolled.
    /// </summary>
    private async Task AssertEnrolledAsync(
        Guid?             studentProfileId,
        Guid              subjectId,
        CancellationToken ct)
    {
        if (studentProfileId is null)
            throw new StudentAccessDeniedException("subject", subjectId);

        var isEnrolled = await _db.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.StudentId == studentProfileId
                        && e.SubjectId == subjectId
                        && e.Status == EnrollmentStatus.Active, ct);

        if (!isEnrolled)
            throw new StudentAccessDeniedException("subject", subjectId);
    }
}
