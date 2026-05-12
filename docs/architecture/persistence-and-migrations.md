# Persistence and Migrations

Established in Phase 1, Section 4, Parts 2–4.

---

## Technology

- **ORM**: Entity Framework Core 8.0.x
- **Driver**: Npgsql.EntityFrameworkCore.PostgreSQL 8.0.x
- **Database**: PostgreSQL 16 (Docker image `postgres:16-alpine`)
- **Migration tool**: `dotnet-ef` global tool

---

## Connection String

| Environment | Source | Value |
|-------------|--------|-------|
| Development (local) | `appsettings.Development.json` | `Host=localhost;Port=5432;Database=lms_dev;Username=lms;Password=lms` |
| Docker Compose | env var `ConnectionStrings__Default` | `Host=postgres;Port=5432;Database=lms_dev;Username=lms;Password=lms` |
| Production | env var `ConnectionStrings__Default` | Injected via secrets manager (never committed) |

The backend throws `InvalidOperationException` at startup if the connection string is missing.

---

## DbContext: `LmsDbContext`

Located at `backend/src/LMS.Infrastructure/Persistence/LmsDbContext.cs`.

- Implements `ILmsDbContext` (Application layer interface — keeps Infrastructure out of Application).
- `OnModelCreating` uses `ApplyConfigurationsFromAssembly` — all `IEntityTypeConfiguration<T>` in the Infrastructure assembly are discovered automatically.
- `SaveChangesAsync` override sets `CreatedAt` if it is still `default` as a defensive audit hook.

### DbSets (12)

`Users` · `Grades` · `Subjects` · `Chapters` · `Lessons` · `Questions`
`ParentProfiles` · `StudentProfiles` · `Enrollments` · `LessonProgress`
`AiConversations` · `AiMessages`

---

## Entity Configurations

One `IEntityTypeConfiguration<T>` file per entity in `LMS.Infrastructure/Persistence/Configurations/`.

### Naming convention

| Convention | Pattern | Example |
|------------|---------|---------|
| Table names | `snake_case` plural | `lesson_progress` |
| Foreign key names | `fk_{table}_{target}` | `fk_lessons_chapter` |
| Index names | `ix_{table}_{columns}` | `ix_chapters_subject_grade_order` |
| Enum storage | `varchar(50)` string | `'Active'`, `'Student'` |
| Audit timestamps | `timestamptz` | always UTC |
| Large text columns | `text` (no max) | `Lesson.Content`, `Question.OptionsJson` |

### Key index decisions

| Index | Purpose |
|-------|---------|
| `ix_users_email` (unique) | Fast login lookup; normalised to lowercase before insert |
| `ix_grades_level_tenant` (unique) | Prevents duplicate grade levels per tenant |
| `ix_subjects_slug_tenant` (unique) | Prevents duplicate slugs per tenant |
| `ix_chapters_subject_grade_order` (unique) | Prevents duplicate chapter order within subject+grade |
| `ix_lessons_chapter_order` (unique) | Prevents duplicate lesson order within chapter |
| `ix_enrollments_student_subject_active` (filtered unique) | One active enrollment per student per subject; allows historical dropped/completed rows |
| `ix_lesson_progress_student_lesson` (unique) | One progress record per student per lesson |

---

## Data Access Pattern

Application services inject `ILmsDbContext` and use EF Core LINQ directly.

```csharp
// ── Read (projected — preferred) ─────────────────────────────────────
var names = await _db.Subjects
    .Where(s => s.TenantId == null)
    .Select(s => new { s.Id, s.Name })
    .ToListAsync(ct);

// ── Read with navigation property ────────────────────────────────────
var chapters = await _db.Chapters
    .Include(c => c.Subject)
    .Where(c => c.GradeId == gradeId)
    .OrderBy(c => c.Order)
    .ToListAsync(ct);

// ── Write ─────────────────────────────────────────────────────────────
var lesson = Lesson.Create(chapterId, "My Lesson", 1, content);
_db.Lessons.Add(lesson);
await _db.SaveChangesAsync(ct);

// ── Existence check ────────────────────────────────────────────────────
bool exists = await _db.Enrollments
    .AnyAsync(e => e.StudentId == studentId && e.SubjectId == subjectId, ct);
```

**Rules:**
- Use `AsNoTracking()` on read-only queries.
- Use `AnyAsync` for existence checks — never fetch just to check.
- Do not filter in memory — push `Where` to SQL.
- Do not loop and query — batch with joins or `Include`.
- Call `SaveChangesAsync` once per logical operation, not after each entity add.

### `IUserRepository`

The only per-entity repository abstraction. It exists because `AuthService` predates `ILmsDbContext`
and `IUserRepository` is a well-defined auth boundary.

Future application services should inject `ILmsDbContext` directly — do not create additional
per-entity repositories unless a service genuinely needs to be tested independently of EF Core.

---

## Migrations

All migrations live in `backend/src/LMS.Infrastructure/Migrations/`.

### Current migration

| Migration | Description |
|-----------|-------------|
| `20260511111052_InitialCreate` | Full schema — all 12 tables, indexes, and FK constraints |

### Adding a new migration

```bash
cd backend
dotnet ef migrations add <MigrationName> \
  --project src/LMS.Infrastructure \
  --startup-project src/LMS.Api
```

### Applying migrations

Migrations are applied automatically at API startup via `DatabaseSeeder.SeedAsync`:

```csharp
await _db.Database.MigrateAsync(ct);
```

This runs in Development only. In staging/production, run migrations explicitly as a pre-deploy step:

```bash
dotnet ef database update \
  --project src/LMS.Infrastructure \
  --startup-project src/LMS.Api
```

### Design-time factory

`LmsDbContextFactory` (`IDesignTimeDbContextFactory<LmsDbContext>`) provides the `dotnet ef` tool
with a hardcoded local connection string so `dotnet ef migrations add` works without a running API.
This factory is **never** used at runtime.

---

## Integration Test Strategy

Tests use **EF Core InMemory** provider via `LmsWebApplicationFactory`:

- Replaces the PostgreSQL `DbContextOptions<LmsDbContext>` registration.
- Sets environment to `"Testing"` so `SeedDatabaseAsync` (which calls `MigrateAsync`) is skipped.
  InMemory does not support `MigrateAsync`.
- Injects a valid JWT secret (32+ chars) — the `"Testing"` environment does not load
  `appsettings.Development.json`.

Each `IClassFixture<LmsWebApplicationFactory>` shares one InMemory database instance across all
tests in the class. Tests must use distinct identifiers (e.g. unique email addresses) to avoid
cross-test state leakage.

---

## Intentionally Deferred

| Concern | Reason deferred |
|---------|----------------|
| Global `TenantId` query filter | Requires `ICurrentUserService` at model-building time — Phase 3 |
| `CreatedBy` / `UpdatedBy` population | Requires `ICurrentUserService` in `SaveChangesAsync` — Phase 3 |
| Production migration pipeline | CI/CD pre-deploy `dotnet ef database update` step — Phase 3 |
| PostgreSQL health check in ASP.NET Core health endpoint | `AddNpgsql(connString)` — Phase 3 |
| Read replicas / CQRS read models | Not needed at current scale — Phase 4+ |
| Outbox pattern for domain events | Not implemented — Phase 4+ |
