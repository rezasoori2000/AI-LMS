# Architecture Overview

AI-LMS is a **multi-tenant, AI-assisted Learning Management System** designed for children,
parents, teachers, and school administrators. This document describes the high-level system
architecture established in Phase 1, Section 1.

---

## System Boundaries

```
┌───────────────────────────────────────────────────────────────┐
│  Client Browser                                               │
│  React SPA (Vite + TypeScript + Tailwind)                     │
│  Port 5173 (dev) / 80 (prod nginx)                           │
└──────────────────────────┬────────────────────────────────────┘
                           │  HTTP / REST
                           ▼
┌───────────────────────────────────────────────────────────────┐
│  API Gateway (Phase 3+)                                       │
│  Nginx / Traefik — TLS termination, rate limiting, routing    │
└──────────┬────────────────────────────────────────────────────┘
           │
    ┌──────┴──────┐
    │             │
    ▼             ▼
┌─────────┐  ┌────────────┐
│ LMS API │  │ AI Service │
│ :5000   │  │ :8000      │
│ .NET 8  │  │ FastAPI    │
│ ASP.NET │  │ Python 3.12│
└────┬────┘  └─────┬──────┘
     │             │
     ▼             ▼
┌─────────┐  ┌────────────┐
│ Postgres│  │  Qdrant    │
│  (L3+)  │  │  (L2+)     │
└─────────┘  └────────────┘
```

---

## Service Responsibilities

### Frontend (React SPA)

| Concern | Approach |
|---------|----------|
| Routing | React Router v6 — client-side SPA routing |
| Server state | TanStack Query — caching, refetch, loading/error states |
| Client state | Zustand (Phase 2+) |
| Styling | Tailwind CSS utility classes |
| Internationalisation | i18next — en default, RTL support built in from day one |
| API communication | Axios with a central `api-client.ts` instance |
| Auth tokens | Stored in memory / `httpOnly` cookie (Phase 2) — never `localStorage` |

### Backend (LMS.Api — ASP.NET Core 8)

Follows **Clean Architecture**. Dependency direction is strictly inward:

```
LMS.Api  →  LMS.Application  →  LMS.Domain
         ↘  LMS.Infrastructure  ↗
```

| Layer | Responsibility |
|-------|---------------|
| **Domain** | Entities, value objects, domain events, business rules. No framework dependencies. |
| **Application** | Use-cases (MediatR Commands/Queries), DTOs, port interfaces (`ICurrentUserService`, etc.) |
| **Infrastructure** | EF Core DbContext, repositories, external service adapters. Implements application ports. |
| **Api** | Controllers, middleware, DI composition root. HTTP concern only. |

Current middleware pipeline (ordered):
1. `ExceptionHandlingMiddleware` — RFC 7807 Problem Details, never leaks stack traces
2. Swagger UI — development only
3. HTTPS redirect
4. CORS (`DefaultPolicy` — origins from config)
5. `TenantMiddleware` — placeholder, Phase 2
6. Authentication / Authorization — Phase 2
7. Controllers + `/health` endpoint

### AI Service (FastAPI — Python 3.12)

Handles all LLM orchestration. The backend never calls LLM providers directly.

| Layer | Responsibility |
|-------|---------------|
| **providers/** | `BaseLLMProvider` ABC — complete / stream / embed. One concrete class per provider (Ollama, OpenAI, …) |
| **services/** | Business logic: tutoring, RAG retrieval, scoring, insights, recommendations |
| **api/v1/** | FastAPI routers — thin HTTP layer, delegates to services |
| **core/** | Config (Pydantic Settings), structured logging |

API documentation (`/docs`, `/redoc`) is only mounted when `DEBUG=true`.

---

## Multi-Tenancy (Phase 2)

Tenants are isolated at the data layer. Each API request carries a **tenant identifier**
resolved by `TenantMiddleware` via one of:

- Subdomain: `tenantA.app.example.com`
- Header: `X-Tenant-Id: <id>`
- JWT claim: `tid` in the access token

All database queries are scoped to the resolved tenant. Row-level security (Postgres RLS)
is a Phase 3 hardening option.

---

## Authentication (Phase 2)

- Backend: JWT Bearer tokens issued by an internal or external IdP (Keycloak / Auth0 / custom).
- Tokens are short-lived (15 min access / 7 day refresh).
- `ICurrentUserService` in the Application layer provides `UserId`, `Email`, `IsAuthenticated`
  without coupling business logic to ASP.NET Core identity types.
- Frontend: tokens stored in memory; refresh token in `httpOnly` cookie (XSS-resistant).

---

## Data Storage (Phase 3+)

| Store | Technology | Purpose |
|-------|-----------|---------|
| Relational | PostgreSQL | Tenants, users, courses, assessments, progress |
| Vector | Qdrant | Embeddings for RAG-based tutoring |
| Cache / PubSub | Redis | Session cache, job queues, real-time events |

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Clean Architecture in backend | Testability — domain/application layers have no framework dependencies |
| Separate AI service | Allows independent scaling; Python ecosystem for ML/AI libs; swap LLM without touching business API |
| All config from env vars | 12-factor app; no secrets in code or images |
| Swagger dev-only | Prevents API schema leakage in production |
| Non-root Docker containers | Defence-in-depth; reduces blast radius from container escapes |
| Never `*` in CORS | Explicit origin allowlist prevents cross-origin credential theft |
| `public partial class Program` | Exposes entry-point to `WebApplicationFactory` for integration tests without breaking encapsulation |

---

## Decisions Deferred

- API Gateway routing rules (Phase 3)
- Observability stack — Prometheus + Grafana (Phase 4)
- Message broker — RabbitMQ / Redis Streams (Phase 3)
- Vector search indexing pipeline (Phase 2)
- Kubernetes manifests (Phase 5)

For detailed per-decision rationale, see [`docs/decisions/`](../decisions/).
