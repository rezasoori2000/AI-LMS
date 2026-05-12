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

## Documentation

### Architecture
- [Architecture Overview](docs/architecture/architecture-overview.md)
- [Domain Model Overview](docs/architecture/domain-model-overview.md)
- [Persistence and Migrations](docs/architecture/persistence-and-migrations.md)

### Development
- [Local Development Guide](docs/development/local-development.md)
- [Folder Structure](docs/development/folder-structure.md)
- [Seed Data Guide](docs/development/seed-data.md)
- [Phase 1 Section 1 Readiness Checklist](docs/development/phase1-section1-readiness.md)
- [Phase 1 Section 2 Readiness Checklist](docs/development/phase1-section2-readiness.md)
- [Phase 1 Section 3 Readiness Checklist](docs/development/phase1-section3-readiness.md)
- [Phase 1 Section 4 Readiness Checklist](docs/development/phase1-section4-readiness.md)
- [Phase 1 Section 5 Readiness Checklist](docs/development/phase1-section5-readiness.md)

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
