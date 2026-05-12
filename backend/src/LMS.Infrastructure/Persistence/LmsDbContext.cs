using LMS.Application.Common.Interfaces;
using LMS.Domain.Catalog;
using LMS.Domain.Common;
using LMS.Domain.Conversations;
using LMS.Domain.Curriculum;
using LMS.Domain.Enrollments;
using LMS.Domain.Progress;
using LMS.Domain.Students;
using LMS.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Persistence;

/// <summary>
/// The EF Core database context for the LMS platform.
///
/// Design notes:
/// - <see cref="ApplyConfigurationsFromAssembly"/> discovers all
///   <see cref="IEntityTypeConfiguration{TEntity}"/> classes in this assembly automatically.
///   Add new configurations to the Configurations/ folder; no registration needed here.
/// - DbSet properties use expression-bodied form so EF Core still discovers them via
///   <see cref="Set{T}"/> while keeping null-safety in C# nullable context.
/// - <see cref="SaveChangesAsync"/> auto-sets <see cref="AuditableEntity.CreatedAt"/>
///   as a defensive safety net (domain factories already set it, but this protects
///   against any entity created outside the factory pattern).
/// - Phase 3: add a global query filter for TenantId (soft multi-tenancy).
/// - Phase 3: promote the SaveChanges interceptor to also capture CreatedBy/UpdatedBy
///   from ICurrentUserService.
/// </summary>
public sealed class LmsDbContext : DbContext, ILmsDbContext
{
    public LmsDbContext(DbContextOptions<LmsDbContext> options) : base(options) { }

    // ── DbSets ────────────────────────────────────────────────────────────────

    public DbSet<User>           Users           => Set<User>();
    public DbSet<Grade>          Grades          => Set<Grade>();
    public DbSet<Subject>        Subjects        => Set<Subject>();
    public DbSet<Chapter>        Chapters        => Set<Chapter>();
    public DbSet<Lesson>         Lessons         => Set<Lesson>();
    public DbSet<Question>       Questions       => Set<Question>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<ParentProfile>  ParentProfiles  => Set<ParentProfile>();
    public DbSet<Enrollment>     Enrollments     => Set<Enrollment>();
    public DbSet<LessonProgress> LessonProgress  => Set<LessonProgress>();
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();
    public DbSet<AiMessage>      AiMessages      => Set<AiMessage>();

    // ── Model configuration ───────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Discovers and applies every IEntityTypeConfiguration<T> in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LmsDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    // ── Save hooks ────────────────────────────────────────────────────────────

    /// <summary>
    /// Defensive audit hook: ensures CreatedAt is never left at default (DateTime.MinValue)
    /// for AuditableEntity instances that bypassed the static Create() factory.
    /// Domain factories already set this; this is purely a safety net.
    /// </summary>
    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>()
                     .Where(e => e.State == EntityState.Added))
        {
            if (entry.Entity.CreatedAt == default)
                entry.Property(nameof(AuditableEntity.CreatedAt)).CurrentValue
                    = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
