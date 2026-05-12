using LMS.Domain.Catalog;
using LMS.Domain.Conversations;
using LMS.Domain.Curriculum;
using LMS.Domain.Enrollments;
using LMS.Domain.Progress;
using LMS.Domain.Students;
using LMS.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core database context.
///
/// Why this interface exists:
///   Application layer services need to query and mutate the database, but they must not
///   depend directly on LmsDbContext (which lives in Infrastructure).  This interface
///   exposes just enough surface area for Application services to do their work while
///   keeping the dependency arrow pointing inward (Application → Infrastructure is forbidden).
///
/// Design decisions:
///   - DbSet&lt;T&gt; properties expose the full EF Core DbSet API (LINQ, async extensions,
///     Add/Remove, etc.) so Application services can write natural LINQ queries.
///   - The Application project references Microsoft.EntityFrameworkCore as a package,
///     which is an accepted pragmatic trade-off for a team that will never swap ORMs.
///     The benefit — async LINQ (ToListAsync, FirstOrDefaultAsync, AnyAsync, Include) —
///     outweighs the theoretical purity concern.
///   - SaveChangesAsync is declared here so services can flush changes without knowing
///     about the concrete DbContext type.
///
/// Coding conventions for Application services that inject this interface:
///
///   // ── Read ──────────────────────────────────────────────────────────────
///   var grades = await _db.Grades
///       .Where(g => g.TenantId == null)
///       .OrderBy(g => g.Level)
///       .ToListAsync(ct);
///
///   // ── Read with projection (preferred — avoids over-fetching) ───────────
///   var names = await _db.Subjects
///       .Where(s => s.TenantId == null)
///       .Select(s => new { s.Id, s.Name })
///       .ToListAsync(ct);
///
///   // ── Read with navigation property ─────────────────────────────────────
///   var chapters = await _db.Chapters
///       .Include(c => c.Subject)
///       .Where(c => c.GradeId == gradeId)
///       .OrderBy(c => c.Order)
///       .ToListAsync(ct);
///
///   // ── Write ─────────────────────────────────────────────────────────────
///   _db.Grades.Add(grade);                    // stage the add
///   await _db.SaveChangesAsync(ct);            // flush to DB
///
///   // ── Existence check ────────────────────────────────────────────────────
///   bool exists = await _db.Enrollments.AnyAsync(
///       e => e.StudentId == studentId &amp;&amp; e.SubjectId == subjectId, ct);
///
/// What to avoid:
///   - Do NOT call SaveChangesAsync after every single operation.
///     Stage all changes for a logical unit of work, then flush once.
///   - Do NOT fetch entities just to check their existence — use AnyAsync.
///   - Do NOT fetch entire sets to filter in memory — push filters to SQL via Where.
///   - Do NOT nest queries inside loops (N+1 problem) — use a single query with joins/Include.
///
/// Deferred:
///   - Phase 3: global TenantId query filter applied on LmsDbContext.OnModelCreating.
///     After that, tenant-aware filtering is automatic; services do not need Where(tenantId).
///   - Phase 3: CreatedBy/UpdatedBy set automatically via ICurrentUserService interceptor.
/// </summary>
public interface ILmsDbContext
{
    // ── Entity sets ───────────────────────────────────────────────────────────

    DbSet<User>           Users           { get; }
    DbSet<Grade>          Grades          { get; }
    DbSet<Subject>        Subjects        { get; }
    DbSet<Chapter>        Chapters        { get; }
    DbSet<Lesson>         Lessons         { get; }
    DbSet<Question>       Questions       { get; }
    DbSet<StudentProfile> StudentProfiles { get; }
    DbSet<ParentProfile>  ParentProfiles  { get; }
    DbSet<Enrollment>     Enrollments     { get; }
    DbSet<LessonProgress> LessonProgress  { get; }
    DbSet<AiConversation> AiConversations { get; }
    DbSet<AiMessage>      AiMessages      { get; }

    // ── Unit of work ──────────────────────────────────────────────────────────

    /// <summary>
    /// Flushes all staged changes to the database in a single transaction.
    /// Call once per logical operation after all entity mutations are complete.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
