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

## Documentation

- [Architecture Overview](docs/architecture/architecture-overview.md)
- [Local Development Guide](docs/development/local-development.md)
- [Folder Structure](docs/development/folder-structure.md)
- [Phase 1 Section 1 Readiness Checklist](docs/development/phase1-section1-readiness.md)
- [Phase 1 Section 2 Readiness Checklist](docs/development/phase1-section2-readiness.md)
- [Frontend UI Foundation](docs/frontend/ui-foundation.md)
- [Routing and Layouts](docs/frontend/routing-and-layouts.md)
- [Styling and Theming](docs/frontend/styling-and-theming.md)
- [Component Conventions](docs/frontend/component-conventions.md)
- [Contributing](CONTRIBUTING.md)

---

## License

Proprietary. All rights reserved.
