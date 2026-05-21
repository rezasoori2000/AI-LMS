# Development Seed Data

Established in Phase 1, Section 4, Part 3.

---

## Purpose

The seed data provides a complete, self-consistent dataset for local development and feature
testing.  It covers every domain entity so any Phase 1 feature (browse curriculum, enroll,
track progress, AI chat) can be exercised immediately after `docker compose up`.

**Never runs in staging or production** — the seeder checks `IsDevelopment()` before doing anything.

---

## Seed Accounts

All seed accounts share the password **`Seed@1234!`**.

| Email | Role | Notes |
|-------|------|-------|
| `superadmin@lms.dev` | SuperAdmin | Platform-level, no tenant |
| `admin@lms.dev` | TenantAdmin | |
| `editor@lms.dev` | ContentEditor | |
| `teacher@lms.dev` | Teacher | No students assigned — use admin UI to assign |
| `parent.a@lms.dev` | Parent | Linked to student.a |
| `parent.b@lms.dev` | Parent | Linked to student.b |
| `student.a@lms.dev` | Student | Enrolled in Mathematics + English; has progress records |
| `student.b@lms.dev` | Student | Enrolled in Mathematics only |

> **Note on teacher accounts**: `teacher@lms.dev` logs in successfully but sees an empty
> student list by default. Teacher–student assignments are admin-managed and not included
> in the seed data (deferred to Phase 3 automatic seeding). To test the teacher portal,
> log in as `admin@lms.dev` and assign a student via the Students page.

---

## Seed Content

| Entity | Count | Details |
|--------|-------|---------|
| Grades | 3 | Grade 5, Grade 6, Grade 7 |
| Subjects | 2 | Mathematics, English Language Arts |
| Chapters | 4 | 2 per subject (Grade 5 only) |
| Lessons | 8 | 2 per chapter; full Markdown content |
| Questions | 16 | 2 per lesson — MultipleChoice, TrueFalse, ShortAnswer mix |
| ParentProfiles | 2 | One per parent user |
| StudentProfiles | 2 | One per student user; linked to Grade 5 and respective parent |
| Enrollments | 3 | Student A → Math + English; Student B → Math |
| LessonProgress | 2 | Student A: Lesson 1 `InProgress`, Lesson 2 `Completed` (score 85%) |

---

## How It Works

1. `Program.cs` calls `await app.SeedDatabaseAsync()` after `app.Build()` and before `app.Run()`.
2. `SeedDatabaseAsync` (in `ApplicationBuilderExtensions`) returns immediately if not `IsDevelopment()`.
3. It creates a scoped DI scope and calls `DatabaseSeeder.SeedAsync()`.
4. `SeedAsync` runs `await _db.Database.MigrateAsync()` first — migrations are always up to date
   before seed inserts run.
5. Each seed section checks `AnyAsync()` before inserting — the seeder is safe to call on every
   startup; it is a no-op once data exists.
6. If the database is unreachable (e.g. unit test run without Postgres), the exception is caught
   and logged as a warning; API startup continues normally.

### Fixed GUIDs

Seed entities use hard-coded GUIDs (e.g. `00000001-0000-0000-0000-000000000001` for SuperAdmin).
This ensures FK relationships are deterministic across seed runs and across developer machines.

The seeder injects these IDs via reflection on the `Entity.Id` backing field — this is seed-only
code and never used in application logic.

---

## Running Locally

```bash
# Start PostgreSQL
docker compose up postgres -d

# Start the API — seed runs automatically
cd backend
dotnet run --project src/LMS.Api

# Or via Docker Compose
docker compose up --build
```

---

## Resetting Seed Data

To reset to a clean seed state, drop and recreate the database:

```bash
docker compose down -v        # removes the postgres_data volume
docker compose up postgres -d
```

The next `dotnet run` will re-apply migrations and re-seed.

---

## Important Notes

- Seed users are **not** visible to the JWT auth flow in integration tests — tests use the InMemory
  provider with a fresh empty database per fixture.  Register test users in test arrange steps.
- The `teacher@lms.dev` account has no `TeacherProfile` entity — teacher classroom features are
  Phase 3.  The user record exists so role-based auth tests can exercise the Teacher role.
- No `AiConversation` records are seeded — conversations are created at runtime by students.
