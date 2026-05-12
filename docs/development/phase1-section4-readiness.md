# Phase 1 — Section 4 Readiness Checklist

Use this checklist before starting **Phase 1, Section 5**.
Every item should be verified in a current checkout of the repo.

---

## Automated checks (run these first)

```bash
cd backend
dotnet build LMS.sln          # must succeed, 0 errors, 0 warnings
dotnet test LMS.sln           # must pass: 96 tests (70 domain + 12 application + 14 API)

cd frontend
npm run type-check             # must exit 0, 0 TS errors
npm run lint                   # must exit 0
npm run test -- --run          # must pass: 41 tests

cd ai-service
pytest                         # must pass: 4 tests
```

**Verified ✅ — Phase 1 Section 4 Parts 1–5 complete (2026-05-11)**
`dotnet build`: 0 errors, 0 warnings
`dotnet test`: 96/96 passing

---

## Domain model

- [x] `User`, `ParentProfile`, `StudentProfile` — identity and profile entities
- [x] `Grade`, `Subject`, `Chapter`, `Lesson`, `Question` — full curriculum hierarchy
- [x] `Enrollment`, `LessonProgress` — learning activity entities with state-machine methods
- [x] `AiConversation`, `AiMessage` — AI tutoring session aggregate
- [x] All entities use private constructor + static `Create()` factory
- [x] `AuditableEntity` base class on all entities except `AiMessage`
- [x] `UserRole` enum stored as string in DB
- [x] `Enrollment.Complete()`, `Drop()`, `LessonProgress.Start()`, `Complete()` — domain behaviour methods tested

## EF Core setup

- [x] `LmsDbContext` with 12 `DbSet<T>` properties
- [x] `ApplyConfigurationsFromAssembly` — new entity configs are auto-discovered
- [x] 12 `IEntityTypeConfiguration<T>` files — one per entity
- [x] `SaveChangesAsync` override — defensive `CreatedAt` audit hook
- [x] `LmsDbContextFactory` — design-time factory for `dotnet ef` tool
- [x] `InitialCreate` migration — all 12 tables, all FK constraints, all indexes

## Schema correctness

- [x] All table names are `snake_case` plural
- [x] All FK constraint names follow `fk_{table}_{target}`
- [x] All index names follow `ix_{table}_{columns}`
- [x] Enum columns are `varchar(50)` strings
- [x] All large text fields use `text` type (no arbitrary max)
- [x] All timestamps are `timestamptz` (UTC)
- [x] Filtered unique index on `enrollments` — allows historical dropped/completed rows
- [x] `ScorePercent` is `decimal(5,2)` — no floating-point precision issues

## PostgreSQL wiring

- [x] `ConnectionStrings:Default` required at startup (throws if missing)
- [x] `appsettings.Development.json` has local Postgres connection string
- [x] `docker-compose.yml` has `postgres:16-alpine` service with healthcheck
- [x] Backend `depends_on: postgres: condition: service_healthy`
- [x] `docker-compose.override.yml` injects `ConnectionStrings__Default` for Docker networking

## Data access layer

- [x] `ILmsDbContext` interface in Application layer — exposes all DbSets and `SaveChangesAsync`
- [x] `LmsDbContext` implements `ILmsDbContext`
- [x] `ILmsDbContext` registered in DI: `AddScoped<ILmsDbContext>(sp => sp.GetRequiredService<LmsDbContext>())`
- [x] `UserRepository` (EF Core) replaces `InMemoryUserRepository` in DI
- [x] `InMemoryUserRepository` retained but marked `[Obsolete]` — not registered in DI

## Development seed data

- [x] `DatabaseSeeder` — idempotent, dev-only, runs `MigrateAsync` first
- [x] 8 seed users covering all roles; password `Seed@1234!`
- [x] Seed content: 3 grades, 2 subjects, 4 chapters, 8 lessons, 16 questions
- [x] Seed learning activity: 2 parent profiles, 2 student profiles, 3 enrollments, 2 progress records
- [x] `SeedDatabaseAsync()` — try-catch wraps seed so a missing DB logs a warning instead of crashing the API
- [x] Seed not called in `"Testing"` environment — integration tests start with empty InMemory DB

## Integration tests

- [x] `LmsWebApplicationFactory` — replaces Npgsql with InMemory, sets `"Testing"` env, injects JWT secret
- [x] Both `AuthIntegrationTests` and `HealthIntegrationTests` use `LmsWebApplicationFactory`
- [x] 14 API integration tests — all passing against InMemory EF Core

---

## Pre-Section 5 checklist

Before starting Section 5 (admin CRUD / content management), confirm:

- [ ] `docker compose up postgres -d && dotnet run --project src/LMS.Api` starts without errors
- [ ] `GET /health` returns 200 Healthy
- [ ] `POST /api/auth/login` with `admin@lms.dev` / `Seed@1234!` returns a valid JWT (requires Postgres running)
- [ ] Swagger UI loads at `http://localhost:5000/swagger`
- [ ] `dotnet test LMS.sln` still passes: 96/96

---

## Section 4 completion summary

### What was built

| Part | Description |
|------|-------------|
| Part 1 | 16 domain entities across 8 namespaces; 70 domain unit tests |
| Part 2 | `LmsDbContext`, 12 EF Core configurations, `InitialCreate` migration, Postgres Docker wiring |
| Part 3 | `DatabaseSeeder` — idempotent dev seed: 8 users, full curriculum content, enrollments, progress |
| Part 4 | `ILmsDbContext` interface, `UserRepository` (EF Core), `LmsWebApplicationFactory` for tests |
| Part 5 | Schema validation, 3 architecture docs, this readiness checklist, README Section 4 summary |

### Test counts

| Project | Tests | Status |
|---------|-------|--------|
| LMS.Domain.Tests | 70 | ✅ All passing |
| LMS.Application.Tests | 12 | ✅ All passing |
| LMS.Api.Tests | 14 | ✅ All passing |
| **Total** | **96** | **✅** |

### Key decisions made in Section 4

- EF Core referenced from the Application layer (via `ILmsDbContext`) — pragmatic trade-off for async LINQ
- No per-entity repository proliferation — Application services use `ILmsDbContext` directly
- Seed uses reflection to inject fixed GUIDs — seed-only, never in application code
- Integration tests use InMemory provider — no external Postgres dependency in CI

### What is intentionally deferred

| Feature | Phase |
|---------|-------|
| Global `TenantId` query filter | 3 |
| `CreatedBy` / `UpdatedBy` auto-population | 3 |
| `TeacherProfile` entity and classroom model | 3 |
| `ParentStudentLink` M:M join table | 3 |
| Assessment `Attempt` / `AttemptAnswer` tables | 3 |
| Production migration pipeline in CI/CD | 3 |
| Lesson `ContentBlocks` structured table | 3 |
| Curriculum standard tags | 3 |
| Read models / CQRS projections | 4+ |
