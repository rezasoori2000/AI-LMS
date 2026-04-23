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
- [ ] Frontend scaffold (Part 3)
- [ ] Backend scaffold (Part 4)
- [ ] AI service scaffold (Part 5)
- [ ] Docker local development (Part 6)
- [ ] Testing scaffold and documentation (Part 7)

**Deferred to later sections/phases:**
- Auth and role-based access control
- Tenant isolation middleware
- Database setup and EF Core migrations
- AI tutoring and RAG
- Lesson flows and assessments
- Multilingual content
- Observability, gateway, and messaging

---

## Documentation

- [Architecture Overview](docs/architecture/architecture-overview.md)
- [Local Development Guide](docs/development/local-development.md)
- [Folder Structure](docs/development/folder-structure.md)
- [Contributing](CONTRIBUTING.md)

---

## License

Proprietary. All rights reserved.
