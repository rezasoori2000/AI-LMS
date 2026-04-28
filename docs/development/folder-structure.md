# Project Folder Structure

This document describes every top-level directory and the conventions used within each
service module. Use it as a map when deciding where new code belongs.

---

## Repository Root

```
AI-LMS/app/
├── ai-service/          # FastAPI Python microservice — LLM orchestration
├── backend/             # ASP.NET Core 8 Web API — domain + application logic
├── docs/                # Project documentation (you are here)
├── frontend/            # React + TypeScript SPA
├── infra/               # Docker, gateway, monitoring, deployment config
├── scripts/             # Automation scripts (DB migrations, setup, deploy)
│
├── .env.example         # Template for local environment variables
├── CONTRIBUTING.md      # Contribution guidelines and PR checklist
├── docker-compose.yml   # Production-like compose (all services)
├── docker-compose.override.yml  # Dev overrides (hot-reload, bind mounts)
├── LICENSE
└── README.md
```

---

## `frontend/`

React 18 SPA built with Vite, TypeScript, and Tailwind CSS.

```
frontend/
├── public/              # Static assets copied to dist/ as-is
├── src/
│   ├── app/
│   │   ├── App.tsx          # Root component — composes providers + router
│   │   └── providers.tsx    # Global providers (React Query, i18n, themes)
│   ├── assets/          # Images, fonts, SVGs imported in code
│   ├── components/      # Shared, reusable UI components (Button, Modal, …)
│   ├── features/        # Feature-sliced modules (each feature is self-contained)
│   │   └── <feature>/
│   │       ├── components/  # UI specific to this feature
│   │       ├── hooks/       # Feature-specific hooks
│   │       └── services/    # API calls for this feature (uses api-client.ts)
│   ├── hooks/           # Generic hooks used across multiple features
│   ├── i18n/
│   │   ├── index.ts         # i18next initialisation
│   │   └── locales/         # Translation JSON files (en.json, ar.json, …)
│   ├── layouts/         # Page layout components (AppLayout, AuthLayout)
│   ├── pages/           # Route-level page components (one per route)
│   ├── routes/
│   │   └── index.tsx        # React Router v6 route definitions
│   ├── services/
│   │   └── api-client.ts    # Axios instance with base URL + interceptors
│   ├── store/           # Global client state (Zustand stores — Phase 2+)
│   ├── tests/
│   │   ├── setup.ts         # Vitest global setup (@testing-library/jest-dom)
│   │   └── utils/           # Test utility helpers (renderWithProviders, …)
│   ├── types/
│   │   └── index.ts         # Shared TypeScript type definitions
│   └── utils/           # Pure utility functions (no side-effects)
│
├── index.html           # Vite entry HTML
├── vite.config.ts       # Vite + Vitest config (aliases, proxy, coverage)
├── tailwind.config.ts
├── tsconfig.json
└── package.json
```

**Key conventions:**
- Import alias `@/` maps to `src/` everywhere.
- Business logic lives in `features/<name>/` — avoid putting domain logic in `components/`.
- API calls are made through `services/api-client.ts`; never call `fetch`/`axios` directly in components.
- All text is internationalised via `i18n/locales/` — never hardcode visible strings.

---

## `backend/`

ASP.NET Core 8 Web API following **Clean Architecture** (Domain → Application → Infrastructure → API).

```
backend/
├── src/
│   ├── LMS.Api/             # ASP.NET Core Web API — HTTP layer only
│   │   ├── Controllers/     # MVC controllers (inherit BaseApiController)
│   │   ├── Extensions/      # DI and pipeline extension methods
│   │   ├── Middleware/       # Custom ASP.NET middleware
│   │   └── Program.cs       # Top-level entry point
│   │
│   ├── LMS.Application/     # Application layer — use-cases, DTOs, contracts
│   │   ├── Common/
│   │   │   └── Interfaces/  # Ports the infrastructure must implement
│   │   └── Features/        # MediatR command/query handlers per feature
│   │
│   ├── LMS.Domain/          # Pure domain model — no framework dependencies
│   │   ├── Common/          # Entity, AuditableEntity base classes
│   │   └── <Aggregate>/     # One folder per aggregate root
│   │
│   └── LMS.Infrastructure/  # EF Core, external services, DI wiring
│       ├── Persistence/     # DbContext, migrations, repositories
│       └── Services/        # Implementations of application interfaces
│
├── tests/
│   ├── LMS.Api.Tests/       # Integration tests (WebApplicationFactory)
│   ├── LMS.Application.Tests/ # Use-case unit tests (mocked infrastructure)
│   └── LMS.Domain.Tests/   # Pure domain logic unit tests
│
├── global.json              # Pins .NET SDK version
└── LMS.sln
```

**Dependency direction:** `Domain` ← `Application` ← `Infrastructure` ← `Api`  
Inner layers never reference outer layers. The `Api` project is the composition root.

---

## `ai-service/`

FastAPI Python microservice that orchestrates LLM calls, RAG pipelines, and AI-powered features.

```
ai-service/
├── app/
│   ├── api/
│   │   ├── router.py        # Root APIRouter; includes versioned sub-routers
│   │   └── v1/
│   │       ├── health.py    # GET /health
│   │       ├── tutor.py     # Tutoring endpoints (Phase 2+)
│   │       └── …
│   ├── core/
│   │   ├── config.py        # Pydantic Settings — all config from env vars
│   │   └── logging.py       # Structured logging setup
│   ├── models/
│   │   └── common.py        # Shared Pydantic response models
│   ├── providers/
│   │   ├── base.py          # BaseLLMProvider ABC (complete / stream / embed)
│   │   ├── registry.py      # get_provider() factory
│   │   └── <name>.py        # Concrete provider (OllamaProvider, etc.) — Phase 2+
│   ├── services/
│   │   ├── base.py          # Service ABCs (tutoring, RAG, scoring, …)
│   │   └── <name>.py        # Concrete service implementations — Phase 2+
│   └── main.py              # create_app() factory + lifespan
│
├── tests/
│   ├── conftest.py          # Shared fixtures (async test client)
│   └── test_health.py       # Health endpoint tests
│
├── pyproject.toml           # Project metadata, ruff config, pytest config
├── requirements.txt         # Runtime dependencies
└── requirements-dev.txt     # Dev/test dependencies (includes requirements.txt)
```

**Key conventions:**
- All configuration is read from environment variables via `app.core.config.Settings`.
- LLM providers are abstracted behind `BaseLLMProvider` — swap providers without changing callers.
- Docs (`/docs`, `/redoc`) are only exposed when `DEBUG=true` to avoid leaking schema in production.

---

## `infra/`

All deployment and infrastructure configuration lives here, separate from application code.

```
infra/
├── docker/
│   ├── frontend.Dockerfile   # Multi-stage: deps → builder → nginx serve
│   ├── backend.Dockerfile    # Multi-stage: restore → build → aspnet runtime
│   ├── ai-service.Dockerfile # Two-stage: venv build → slim runtime
│   └── nginx.conf            # SPA routing + security headers + gzip
├── gateway/             # API gateway config (Nginx/Traefik — Phase 3+)
├── monitoring/          # Prometheus, Grafana dashboards (Phase 4+)
├── messaging/           # RabbitMQ / Redis Streams config (Phase 3+)
├── search/              # Qdrant vector DB config (Phase 2+)
└── deployment/          # Kubernetes manifests / Helm charts (Phase 5+)
```

---

## `docs/`

```
docs/
├── api/             # Generated OpenAPI specs, API reference
├── architecture/    # Architecture Decision Records (ADRs), diagrams
├── decisions/       # Lightweight decision logs (where ADRs are too heavy)
└── development/     # Guides for contributors (this file, local-development.md)
```

---

## `scripts/`

```
scripts/
├── db/      # Database migration helpers, seed scripts
├── deploy/  # CI/CD deploy scripts
└── setup/   # One-time environment setup scripts
```

Scripts are standalone — they must not be required for the application to run.
Each script should document its inputs and side-effects at the top of the file.

---

## Where Does New Code Go?

| I want to add… | Location |
|----------------|----------|
| A new API endpoint | `backend/src/LMS.Api/Controllers/` + use-case in `LMS.Application/Features/` |
| A new domain entity | `backend/src/LMS.Domain/<Aggregate>/` |
| A new React page | `frontend/src/pages/` + route in `frontend/src/routes/index.tsx` |
| A new React feature | `frontend/src/features/<name>/` |
| A new AI capability | `ai-service/app/services/` (service) + `ai-service/app/api/v1/` (endpoint) |
| A new LLM provider | `ai-service/app/providers/<name>.py` + register in `registry.py` |
| Infrastructure config | `infra/<category>/` |
| A new environment variable | Add to `.env.example` with a comment, then consume in the relevant `config` module |
