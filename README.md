# AI-LMS — AI-Powered Educational Platform

An intelligent, multi-tenant educational platform for children, parents, teachers, and schools.
Built with React, ASP.NET Core, and Python FastAPI.

---

## Overview

AI-LMS is a production-grade, AI-assisted learning platform that combines:
- Curriculum-based structured lessons
- AI tutoring with RAG (Retrieval-Augmented Generation)
- Progress tracking and adaptive assessments
- Multi-role access (Super Admin, Tenant Admin, Teacher, Parent, Student)
- Strict multi-tenant isolation
- Multilingual and RTL/LTR support

---

## Technology Stack

| Layer        | Technology                        |
|--------------|-----------------------------------|
| Frontend     | React, TypeScript, Vite, Tailwind CSS, TanStack Query |
| Backend      | ASP.NET Core Web API, C#, EF Core, PostgreSQL |
| AI Service   | Python, FastAPI                   |
| Container    | Docker, Docker Compose            |

---

## Repository Structure

```
app/
├── frontend/        # React + TypeScript + Vite frontend
├── backend/         # ASP.NET Core Web API (Clean Architecture)
├── ai-service/      # Python FastAPI AI service
├── infra/           # Infrastructure configs (gateway, monitoring, deployment)
├── docs/            # Architecture, API, and development documentation
├── scripts/         # Utility and automation scripts
└── .github/         # CI/CD workflows and PR templates
```

See [docs/development/folder-structure.md](docs/development/folder-structure.md) for full details.

---

## Getting Started

### Prerequisites

- Docker Desktop
- Node.js 20+
- .NET 8 SDK
- Python 3.11+

### Run Locally

```bash
# Copy environment variables
cp .env.example .env

# Start all services
docker compose up --build
```

Service URLs (local):

| Service     | URL                        |
|-------------|----------------------------|
| Frontend    | http://localhost:5173      |
| Backend API | http://localhost:5000       |
| AI Service  | http://localhost:8000       |
| Swagger UI  | http://localhost:5000/swagger |

See [docs/development/local-development.md](docs/development/local-development.md) for detailed setup instructions.

---

## Default Language

The platform defaults to **English**. Multilingual and RTL support are built in from the foundation.

---

## Phase 1 — Section 1 Status

**Completed:**
- [x] Monorepo folder structure
- [x] Root-level files and standards
- [x] Frontend scaffold (React 18, Vite, TypeScript, Tailwind, TanStack Query, i18next, RTL)
- [x] Backend scaffold (ASP.NET Core 8, Clean Architecture, Swagger dev-only, health endpoint, ExceptionHandlingMiddleware)
- [x] AI service scaffold (FastAPI, Pydantic Settings, LLM provider/service ABCs, health endpoint)
- [x] Docker local development (multi-stage Dockerfiles, hot-reload, health checks, docker-compose override)
- [x] Testing scaffold and documentation (unit + integration tests for all services, architecture overview, dev guides)

**Deferred to Phase 1, Section 2+:**
- Authentication (JWT / OAuth) and role-based access control
- Tenant isolation middleware and data scoping
- Database setup and EF Core migrations (PostgreSQL)
- AI tutoring, RAG pipeline, and LLM provider implementations
- Lesson flows, assessments, and progress tracking
- Multilingual content management
- API gateway, observability (Prometheus, Grafana), and messaging (Redis/RabbitMQ)

See [Phase 1 Section 1 Readiness Checklist](docs/development/phase1-section1-readiness.md) before starting Section 2.

---

## Phase 1 — Section 2 Status

**Completed:**
- [x] App shell — Sidebar with navigation groups, Topbar, PageContainer with accessible headings
- [x] Route structure — `createBrowserRouter`, per-role route modules, `RouteHandle` (titleKey + access) on every route
- [x] Theme and token system — CSS custom properties for color, radius, shadow, typography; dark-mode and RTL font blocks in `index.css`; Tailwind `tailwind.config.ts` wired to the same vars
- [x] RTL/LTR foundation — `RTL_LOCALES` exported from single source, `applyDirection` sets `lang`/`dir` on `<html>`, logical spacing (`ms-*`/`me-*`) used throughout
- [x] Shared UI primitives — `Button`, `Badge`, `Card`, `Input`, `StatCard`, `SectionCard`, `PlaceholderRow`, `SkeletonBlock`/`SkeletonText`
- [x] Async/state components — `LoadingState`, `EmptyState`, `ErrorState`, `InlineFeedback`, `StateWrapper`
- [x] State component i18n — all default strings resolved via `t()`, no hardcoded English fallbacks
- [x] Feedback component tests — 31 RTL tests for all 5 feedback components (`@testing-library/react`)
- [x] Placeholder dashboards — Admin, Teacher, Parent, Student each with stat rows and structured `SectionCard` sections
- [x] i18n — all nav, section, dashboard, and state-component strings keyed in `en.json`
- [x] Barrel exports for `@/components/ui` and `@/components/feedback`
- [x] Frontend documentation (`docs/frontend/`)

**Build metrics:** 143 modules · JS 325 KB / 101 KB gzip · CSS 20 KB / 4.7 KB gzip · 41/41 tests passing · 0 TS errors

**Deferred to Phase 1, Section 3:**
- Authentication (JWT / OAuth) and role-based access control
- Auth guard route loader (`RouteHandle.access` enforcement)
- Role-filtered navigation (wiring `allowedRoles` to authenticated user)
- `AuthLayout` login/register pages
- React Error Boundary at route level
- Toast/snackbar for transient mutation feedback
- Tenant isolation middleware and data scoping
- Database setup and EF Core migrations

See [Phase 1 Section 2 Readiness Checklist](docs/development/phase1-section2-readiness.md) before starting Section 3.

---

## Phase 1 — Section 3 Status

**Completed:**
- [x] `User` aggregate root — PBKDF2-SHA256 password hashing, JWT HS256 token service
- [x] `AuthService` — register + login, `IUserRepository` abstraction
- [x] `ExceptionHandlingMiddleware` — RFC 7807 Problem Details; 401/409/500 mapped to correct status codes
- [x] Frontend `LoginPage` + `RegisterPage`, `AuthContext`, `ProtectedRoute`, `tokenStorage`
- [x] Session restoration on hard-refresh; session-expired event; `AuthLayout` redirect guard
- [x] 96 passing tests after Section 4 (original Section 3 count: 31 backend, 41 frontend)

**Deferred to Phase 1, Section 4+:**
- EF Core database setup and PostgreSQL migration
- Domain model beyond User

See [Phase 1 Section 3 Readiness Checklist](docs/development/phase1-section3-readiness.md) before starting Section 4.

---

## Phase 1 — Section 4 Status

**Completed:**
- [x] 16 domain entities — User, ParentProfile, StudentProfile, Grade, Subject, Chapter, Lesson, Question, Enrollment, LessonProgress, AiConversation, AiMessage
- [x] `LmsDbContext` with 12 DbSets; `ApplyConfigurationsFromAssembly`; `SaveChangesAsync` audit hook
- [x] 12 `IEntityTypeConfiguration<T>` files — `snake_case` tables, explicit FK names, all indexes
- [x] `InitialCreate` migration — full schema, FK constraints, filtered unique index on enrollments
- [x] PostgreSQL 16 Docker service wired into `docker-compose.yml` with healthcheck
- [x] `DatabaseSeeder` — idempotent dev-only seed: 8 users (all roles), curriculum content, enrollments, progress
- [x] `ILmsDbContext` interface in Application layer; `UserRepository` (EF Core) replaces `InMemoryUserRepository`
- [x] `LmsWebApplicationFactory` — InMemory EF Core, no Postgres required in integration tests
- [x] Architecture docs: domain model overview, persistence/migrations, seed data guide
- [x] **96 passing tests** — 70 domain + 12 application + 14 API — 0 warnings, 0 errors

**Deferred to Phase 1, Section 5+:**
- Admin CRUD endpoints (grades, subjects, chapters, lessons, questions)
- Global `TenantId` query filter (Phase 3)
- `CreatedBy`/`UpdatedBy` auto-population via `ICurrentUserService` (Phase 3)
- `TeacherProfile` entity and classroom model (Phase 3)
- `ParentStudentLink` M:M join table (Phase 3)
- Assessment attempt/answer tables (Phase 3)
- Production migration CI/CD pipeline (Phase 3)

See [Phase 1 Section 4 Readiness Checklist](docs/development/phase1-section4-readiness.md) before starting Section 5.

---

## Phase 1 — Section 5 Status

**Completed:**
- [x] 5 content services — `GradeService`, `SubjectService`, `ChapterService`, `LessonService`, `QuestionService` (Clean Architecture; no AutoMapper; no per-entity repository)
- [x] 5 admin API controllers — full CRUD at `/api/admin/{grades,subjects,chapters,lessons,questions}`; `[Authorize]` + `[RequireRole]`
- [x] `[Required]` / `[MaxLength]` / `[Range]` data annotations on all content request DTOs — invalid input returns 400, never 500
- [x] `ContentNotFoundException` (→ 404) and `ContentConflictException` (→ 409) — handled by `ExceptionHandlingMiddleware`
- [x] 8 admin frontend pages — list + form for all 5 entities (Grades, Subjects, Chapters, Lessons, Questions)
- [x] Type-aware question form — MultipleChoice (4 options + index), TrueFalse, ShortAnswer; changing type resets type-specific fields
- [x] Lesson-question association — `?lessonId=` URL filter on QuestionsPage; "Questions" button per lesson row
- [x] `ConfirmDeleteButton`, `StateWrapper`, `mapApiError` reused across all 5 content entity pages
- [x] i18n coverage — full `admin.content.questions` block; `common.saving` / `common.true` / `common.false` keys
- [x] Consistent "Saving…" button text while mutation is pending across all 5 form pages
- [x] **111 passing tests** — 70 domain + 12 application + 29 API (+15 AdminContent integration tests) · 41 frontend

**Deferred to Phase 1, Section 6+:**
- Publishing workflow (`IsPublished` lifecycle, draft/review/published states) — Phase 3
- Media management (video, file attachments) — Phase 3
- Advanced filtering, search, and server-side pagination — Phase 2
- Audit history (`CreatedBy` / `UpdatedBy` auto-population) — Phase 3
- Content localisation (multilingual lesson/question text) — Phase 3
- Question `Explanation` field, tags, variable option counts, ordering — Phase 3
- Assessment runtime (quiz sessions, attempts, scoring) — Phase 3
- Content service unit tests in `LMS.Application.Tests` — Phase 2
- FluentValidation and client-side Zod validation — Phase 2
- Searchable / ComboBox selects for large dropdowns — Phase 2
- Subject description field in admin form — Phase 2

See [Phase 1 Section 5 Readiness Checklist](docs/development/phase1-section5-readiness.md) before starting Section 6.

---

## Phase 1 — Section 6 Status

**Completed:**
- [x] `ParentService` — read-only, ownership-enforced via `ParentProfile.Id` resolved from JWT claims
- [x] `ParentController` — `GET /api/parent/children`, `GET /api/parent/children/{studentId}`; `[Authorize(Roles = "Parent")]`
- [x] `ParentAccessDeniedException` → 403 Forbidden (privacy-preserving; prevents student ID enumeration)
- [x] `HttpCurrentUserService` — reads JWT `sub`/`email`/`role`/`tid` claims; fully implemented
- [x] `StudentAdminService` + `StudentsController` — admin assigns/unlinks parent via `PATCH /api/admin/students/{id}/parent`
- [x] `StudentsLinkPage` — admin frontend; inline parent assignment with dropdown + save/cancel
- [x] `ParentDashboardPage` — live stat row + children overview connected to `GET /api/parent/children`
- [x] `ChildrenPage` — full children list with grade, enrollment count, lessons completed, last activity
- [x] `ChildDetailPage` — per-subject enrollment + progress summary (total/completed/in-progress/avg score)
- [x] `parent.routes.tsx` — all routes carry `access: ['parent']` role gate
- [x] i18n — full `parent.*` block; `admin.students.*` block; `common.actions` key
- [x] **126 backend tests** (70 domain + 12 application + 44 API) · **41 frontend Vitest tests** — all passing

**Deferred to Phase 2+:**
- `ParentProfile` auto-creation on registration (admin-managed in Phase 1)
- Parent–teacher messaging
- Notifications (grade published, assignment due)
- Parent invitation / self-service onboarding
- Multi-guardian M:M linkage (`ParentStudentLink` table — Phase 3)
- Per-lesson progress timeline and performance trend charts
- Happy-path integration test for parent with linked children
- `formatLastActivity` i18n (hardcoded English in Phase 1)

See [Phase 1 Section 6 Readiness Checklist](docs/development/phase1-section6-readiness.md) before starting Section 7.

---

## Phase 1 — Section 7 Status

**Completed:**
- [x] `StudentService` — 6-method service: summary stats, enrolled subjects, subject chapter tree, lesson detail, start (idempotent), complete with scoring
- [x] `StudentController` — 5 HTTP endpoints at `/api/student/*`; `[Authorize(Roles = "Student")]`
- [x] `StudentAccessDeniedException` → 403 Forbidden (prevents resource enumeration for non-enrolled lessons)
- [x] `LessonAlreadyCompletedException` → 409 Conflict (re-completion returns 409, not 500)
- [x] Answer security — `CorrectAnswer` excluded from `QuestionForStudentDto` at EF Core projection level
- [x] MC + TF auto-scoring; ShortAnswer included but always `isCorrect: false` (AI evaluation deferred)
- [x] `LessonProgress` state machine: NotStarted → InProgress (on lesson open) → Completed (terminal)
- [x] `StudentDashboardPage` — live stat row + continue-learning + subject preview
- [x] `SubjectsPage` — enrolled subjects with completion % and next-lesson shortcut
- [x] `SubjectDetailPage` — chapter-grouped lesson list with human-readable progress badges
- [x] `LessonPlayerPage` — plain text content + question form + submit + score feedback
- [x] `useStudent.ts` — full React Query hook set including `useStartStudentLesson` (fire-and-forget)
- [x] **126 backend tests** · **41 frontend tests** — all passing · `tsc --noEmit` clean

**Phase 1 locked decisions:**
- Free lesson access within enrolled subjects (no sequential gating)
- Partial answer submission allowed
- `isCorrect` only returned — no correct-answer reveal in Phase 1
- Plain text content rendering (no Markdown)
- One attempt per lesson (terminal `Completed` state)

**Deferred to Phase 2+:**
- `StudentProfile` auto-creation on registration (admin-managed in Phase 1)
- Progress page at `/student/progress`
- Student self-enrollment
- Sequential lesson gating / prerequisites
- Re-attempt support (`LessonAttempt` child table — Phase 3)
- ShortAnswer AI evaluation (Phase 3)
- Correct-answer reveal post-completion (Phase 3)
- Per-question answer persistence for review mode (Phase 3)
- Gamification, AI tutoring, advanced analytics (Phase 3+)

See [Phase 1 Section 7 Readiness Checklist](docs/development/phase1-section7-readiness.md) before starting Section 8.

---

## Phase 1 — Section 8 Status

**Completed:**
- [x] `TeacherStudentAssignment` M:M join entity — replaces 1:1 `StudentProfile.TeacherId` FK
- [x] Migration `AddTeacherStudentAssignmentTable` — drops old FK column, creates join table with unique composite index
- [x] `ITeacherService` + `TeacherService` — 4 read methods; ownership enforced via `TeacherStudentAssignment`
- [x] `TeacherController` — 4 endpoints at `/api/teacher/*`; `[Authorize(Roles = "Teacher")]`
- [x] `TeacherAccessDeniedException` → 403 Forbidden (unassigned student access returns 403, not 404)
- [x] Admin teacher assignment API — `GET /api/admin/students/teacher-options`, `PATCH /api/admin/students/{id}/teacher`
- [x] Admin `StudentsLinkPage` — teacher column + inline teacher assignment UI (parent + teacher in one page)
- [x] `TeacherDashboardPage`, `StudentsPage`, `StudentMonitorPage` — full teacher portal frontend
- [x] `teacher.routes.tsx` with `access: ['teacher']` role gate
- [x] **139 backend tests** · **41 frontend tests** — all passing · `tsc --noEmit` clean
- [x] Bug fix: `student-admin.service.ts` double `/api/` URL prefix corrected

**Phase 1 locked decisions:**
- Teacher portal is read-only (no teacher-initiated writes in Phase 1)
- One teacher per student (admin-managed; join table schema supports future M:M)
- `AssignedByUserId` stored on every assignment for audit trail
- No `TeacherProfile` entity in Phase 1 — teacher identity is `User.Id`

**Deferred to Phase 3+:**
- `formatLastActivity` i18n in `StudentMonitorPage` (hardcoded English — same gap as parent portal)
- `TeacherProfile` entity (bio, display name, specialisations)
- Subject-scoped co-teaching (multiple teachers per student per subject)
- Teacher notification on new student assignment
- Teacher self-service for assignment requests

See [Phase 1 Section 8 Readiness Checklist](docs/development/phase1-section8-readiness.md) before starting Section 9.

---

## Phase 1 — Section 9 Status

**Completed:**
- [x] Architecture boundaries locked — `StudentProfile` and `User` carry zero adaptive fields; rule documented and validated
- [x] `QuestionAnswerRecord` domain entity — durable binary correct/incorrect outcome per question per lesson completion
- [x] Migration `AddQuestionAnswerRecordTable` — indexes on `(StudentId, LessonId)` and `(StudentId, QuestionId)`
- [x] `StudentService.CompleteLessonAsync` — persists answer records in the same `SaveChangesAsync` call as `LessonProgress`
- [x] `LMS.Domain/Personalization/DesignNotes.cs` updated — entity shapes aligned with Part 3 design; exclusion list and GDPR deletion order added
- [x] `docs/architecture/domain-model-overview.md` updated — `QuestionAnswerRecord` and `TeacherStudentAssignment` added
- [x] `docs/architecture/personalization-readiness.md` created — authoritative boundary reference for Phase 3 AI tutor work
- [x] Boundary validation passed — no adaptive fields in identity or profile entities; no answer leakage in student DTOs
- [x] **139 backend tests** · **41 frontend tests** — all passing · `tsc --noEmit` clean

**Phase 1 locked decisions:**
- `LearnerProfile` keyed by `(UserId, TenantId)` — durable identity anchor even if `StudentProfile` is recreated
- Observation layer (`LessonProgress`, `QuestionAnswerRecord`, `Enrollment`, `AiConversation`) is read-only from Phase 3
- `LearnerPreferences` is the only personalization entity a student directly edits — no auto-population from inferred signals
- `ConsistencySignal` is a factual activity count — no streak or gamification framing
- `TopicMasterySnapshot` stores numeric ratios only — no inferred concept-weakness string labels

**Known signal quality gap (documented):**
- `ShortAnswer` questions always produce `IsCorrect = false` in Phase 1 (no AI grading). Phase 3 `TopicMasterySnapshot` computation must account for this.

**Deferred to Phase 3+:**
- `LearnerProfile`, `LearnerPreferences`, `TopicMasterySnapshot`, `ConsistencySignal` entity implementation
- `TopicMasterySnapshot` refresh in `CompleteLessonAsync`
- AI context-assembly service
- Vector store integration
- `Question.TopicId` content taxonomy
- `formatLastActivity` i18n (teacher + parent portals)
- Pending migrations apply when Docker is running: `dotnet ef database update --project src/LMS.Infrastructure --startup-project src/LMS.Api`

See [Phase 1 Section 9 Readiness Checklist](docs/development/phase1-section9-readiness.md).

---

## Phase 1 — Section 10 Status

**Platform Audit, Consistency Cleanup, and Phase 1 Readiness Review** — Complete (all 5 parts).

### Part 1 — Audit and critical gaps
- Added `StudentIntegrationTests.cs`: 17 new API integration tests; closed the last zero-coverage gap across all portals.
- Unified exception handling: `AdminLinkValidationException` (→ 400) in Application layer; removed all inline try/catch from `StudentsController`.
- Renamed `ChildDetailResponse` → `ChildDetailDto`: 5 files; naming consistent with all other DTOs in the Application layer.
- Extracted date utilities: `formatDate` and `formatLastActivity` consolidated in `frontend/src/utils/dateFormat.ts`.

### Part 2 — Cross-module cleanup implementation
- Fixed `BackendUserRoleValue` type: changed from integer union to string union matching backend serialization (`"Student"` not `5`). Added `toFrontendRole()` helper.
- Wired role-based nav: `AppLayout` now resolves `NAV_ITEMS_BY_ROLE[toFrontendRole(user.role)]` — each role sees their own sidebar.
- Removed `BaseApiController`: deleted the unused base class; `AuthController` now extends `ControllerBase` directly.
- Normalized enrollment ordering: `ParentService.GetChildDetailAsync` changed to descending, matching `TeacherService`.

### Part 3 — Authorization & ownership enforcement review
- JWT startup fail-fast guard: application refuses to start in Production/Staging if the JWT secret key is absent or shorter than 32 characters.
- Standardized 403 response titles: all `*AccessDeniedException` cases return `"Access denied."` — no resource GUIDs in response bodies.
- Added `[FromBody]` on `AssignParent` and `AssignTeacher` PATCH request parameters in `StudentsController`.

### Part 4 — Test strategy and minimum automated coverage
- Teacher portal: 3 helpers + 4 new tests (happy-path list/detail/progress + cross-teacher isolation).
- Parent portal: 3 helpers + 3 new tests (happy-path children/child-detail + cross-parent isolation).
- Student portal: 1 helper + 3 new tests (`CorrectAnswer` exclusion, `QuestionAnswerRecord` persistence, 100% score).
- Frontend: `dateFormat.test.ts` (NEW) — 6 tests with `vi.useFakeTimers()` for deterministic date assertions.

### Part 5 — Developer experience, local setup, and Section 10 closeout
- Reviewed and validated all local developer workflows (Docker Compose, native run, test commands, migrations).
- Updated `local-development.md`: added JWT secret requirement note, migration quick-reference, and known dev seed limitation.
- Updated `seed-data.md`: noted that `teacher@lms.dev` shows an empty student list by design (assignments are admin-managed).
- Captured consolidated Phase 2 starting point in the Section 10 readiness doc.

### Final test counts
- Backend: **168 tests** (70 domain + 12 application + 86 API integration)
- Frontend: **47 tests** (3 test files)
- All passing; `tsc --noEmit` clean; `dotnet build` 0 warnings

### Phase 2 starting point (first priorities)
- Route-level role enforcement (`RouteGuard` reading `handle.access`) — unblocked by Part 2 type fix
- `formatLastActivity` i18n — translation pipeline work
- `ParentProfile` auto-creation on registration (admin-managed only in Phase 1)
- Server-side pagination and filtering on admin list pages

See [Phase 1 Section 10 Readiness Checklist](docs/development/phase1-section10-readiness.md).

---

## Phase 1 — Section 11 Status

**AI Tutor MVP Boundaries and Architecture Foundations** — Part 1 complete.

### Part 1 — Architecture foundations (current)
- Defined `ITutorService` interface with explicit data ownership docs and hard boundaries.
- Created `TutorContextSnapshot` record encoding the context assembly contract (`CorrectAnswer` absent at the type level).
- Created `TutorBoundaryNotes.cs` — call architecture, MVP scope, data ownership, and safety constraints as co-located code comments.
- Created `TutorController` stub under `[Authorize(Roles = "Student")]` returning HTTP 501.
- Created `ai-service/app/models/tutor.py` Pydantic models and `api/v1/tutor.py` route stub (501).
- Wired tutor router into `ai-service/app/api/router.py`.
- No breaking changes; all 168 backend tests continue to pass.

### Part 2 — Service implementation (next)
- Implement `TutorService`: enrollment check → context assembly → AI service HTTP call → persist `AiConversation` + `AiMessage`
- Implement `ask_tutor` in AI service: lesson-grounded system prompt + LLM `complete()` call
- Wire `HttpClient` in backend for AI service calls
- Add integration tests for start / ask / end lifecycle

### Current test counts
- Backend: **168 tests** (70 domain + 12 application + 86 API integration) — all passing
- Frontend: **47 tests** — all passing

See [Phase 1 Section 11 Readiness](docs/development/phase1-section11-readiness.md).

---

## Documentation

### Architecture
- [Architecture Overview](docs/architecture/architecture-overview.md)
- [Domain Model Overview](docs/architecture/domain-model-overview.md)
- [Persistence and Migrations](docs/architecture/persistence-and-migrations.md)
- [Personalization Readiness](docs/architecture/personalization-readiness.md)

### Student Portal
- [Student Portal MVP](docs/student-portal-mvp.md)

### Parent Portal
- [Parent Portal MVP](docs/parent-portal-mvp.md)
- [Parent–Student Linkage Notes](docs/parent-student-linkage-notes.md)

### Teacher Portal
- [Teacher Portal MVP](docs/teacher-portal-mvp.md)
- [Teacher–Student Assignment Notes](docs/teacher-student-assignment-notes.md)

### Development
- [Local Development Guide](docs/development/local-development.md)
- [Folder Structure](docs/development/folder-structure.md)
- [Seed Data Guide](docs/development/seed-data.md)
- [Phase 1 Section 1 Readiness Checklist](docs/development/phase1-section1-readiness.md)
- [Phase 1 Section 2 Readiness Checklist](docs/development/phase1-section2-readiness.md)
- [Phase 1 Section 3 Readiness Checklist](docs/development/phase1-section3-readiness.md)
- [Phase 1 Section 4 Readiness Checklist](docs/development/phase1-section4-readiness.md)
- [Phase 1 Section 5 Readiness Checklist](docs/development/phase1-section5-readiness.md)
- [Phase 1 Section 6 Readiness Checklist](docs/development/phase1-section6-readiness.md)
- [Phase 1 Section 7 Readiness Checklist](docs/development/phase1-section7-readiness.md)
- [Phase 1 Section 8 Readiness Checklist](docs/development/phase1-section8-readiness.md)
- [Phase 1 Section 9 Readiness Checklist](docs/development/phase1-section9-readiness.md)
- [Phase 1 Section 10 Readiness Checklist](docs/development/phase1-section10-readiness.md)
- [Phase 1 Section 11 Readiness](docs/development/phase1-section11-readiness.md)

### Frontend
- [Frontend UI Foundation](docs/frontend/ui-foundation.md)
- [Routing and Layouts](docs/frontend/routing-and-layouts.md)
- [Styling and Theming](docs/frontend/styling-and-theming.md)
- [Component Conventions](docs/frontend/component-conventions.md)

### Contributing
- [Contributing](CONTRIBUTING.md)

---

## License

Proprietary. All rights reserved.
